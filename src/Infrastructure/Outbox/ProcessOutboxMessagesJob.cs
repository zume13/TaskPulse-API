using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Domain.Shared.DomainEvent;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Infrastructure.Outbox;

/// <summary>
/// Quartz job that runs the handlers of every pending outbox message
/// (creates notifications, sends emails).
///
/// Each run: 1) reserves a batch of due messages with a short lease,
/// 2) processes every message in its own scope and DbContext.
/// For each handler of the event, the handler's database writes and a
/// consumer row are saved together, so a handler that already succeeded is
/// never run again if a later handler fails and the message is retried.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob(
    IServiceScopeFactory scopeFactory,
    OutboxOptions options,
    TimeProvider timeProvider,
    ILogger<ProcessOutboxMessagesJob> logger) : IJob
{
    // Quartz 4 jobs return ValueTask and receive the cancellation token.
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var messageIds = await ClaimBatchAsync(cancellationToken);

        foreach (var messageId in messageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ProcessAsync(messageId, cancellationToken);
        }
    }

    private async Task<List<Guid>> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskPulseDbContext>();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var leaseUntilUtc = nowUtc.AddSeconds(options.LeaseSeconds);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Raw SQL because EF has no SKIP LOCKED. Two app instances running
        // this job at the same time never pick the same row.
        // Materialised with ToListAsync on purpose: composing more LINQ onto
        // this query would wrap the locking clause.
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAtUtc" IS NULL
                  AND "FailedAtUtc" IS NULL
                  AND "NextAttemptAtUtc" <= {nowUtc}
                  AND ("LockedUntilUtc" IS NULL OR "LockedUntilUtc" < {nowUtc})
                ORDER BY "OccurredAtUtc"
                LIMIT {options.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
            message.Lease(leaseUntilUtc);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages.Select(message => message.Id).ToList();
    }

    private async Task ProcessAsync(Guid messageId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskPulseDbContext>();

        var message = await dbContext.OutboxMessages.FirstOrDefaultAsync(
            m => m.Id == messageId && m.ProcessedAtUtc == null && m.FailedAtUtc == null,
            cancellationToken);

        if (message is null)
            return;

        try
        {
            var eventType = DomainEventTypes.Resolve(message.Type)
                ?? throw new InvalidOperationException($"Unknown domain event type '{message.Type}'.");

            var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType, OutboxJson.Options) as IDomainEvent
                ?? throw new InvalidOperationException($"Outbox message {message.Id} has an unreadable payload.");

            await RunHandlersAsync(scope.ServiceProvider, dbContext, message, eventType, domainEvent, cancellationToken);

            message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RecordFailureAsync(dbContext, messageId, exception, cancellationToken);
        }
    }

    private async Task RunHandlersAsync(
        IServiceProvider serviceProvider,
        TaskPulseDbContext dbContext,
        OutboxMessage message,
        Type eventType,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handleMethod = handlerInterface.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle))!;

        var alreadyConsumed = (await dbContext.OutboxMessageConsumers
                .Where(consumer => consumer.OutboxMessageId == message.Id)
                .Select(consumer => consumer.Handler)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var handler in serviceProvider.GetServices(handlerInterface))
        {
            if (handler is null)
                continue;

            var handlerName = handler.GetType().FullName!;

            if (alreadyConsumed.Contains(handlerName))
                continue;

            try
            {
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            }

            // The handler's own writes (e.g. new notifications) and the
            // consumer row are committed by this one SaveChanges.
            dbContext.OutboxMessageConsumers.Add(OutboxMessageConsumer.Create(
                message.Id,
                handlerName,
                timeProvider.GetUtcNow().UtcDateTime));

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RecordFailureAsync(
        TaskPulseDbContext dbContext,
        Guid messageId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            // Drop whatever the failed handler left half-done in memory.
            dbContext.ChangeTracker.Clear();

            var message = await dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message is null)
                return;

            message.MarkFailed(
                $"{exception.GetType().Name}: {exception.Message}",
                timeProvider.GetUtcNow().UtcDateTime,
                options.MaxAttempts);

            await dbContext.SaveChangesAsync(cancellationToken);

            if (message.FailedAtUtc is not null)
            {
                logger.LogError(
                    exception,
                    "Outbox message {MessageId} ({Type}) is dead-lettered after {Attempts} attempts.",
                    message.Id, message.Type, message.Attempts);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Outbox message {MessageId} ({Type}) failed, attempt {Attempts}. Next try at {NextAttemptAtUtc:u}.",
                    message.Id, message.Type, message.Attempts, message.NextAttemptAtUtc);
            }
        }
        catch (Exception recordException)
        {
            // The lease expires on its own and the message is picked up again.
            logger.LogError(recordException, "Could not record the failure of outbox message {MessageId}.", messageId);
        }
    }
}