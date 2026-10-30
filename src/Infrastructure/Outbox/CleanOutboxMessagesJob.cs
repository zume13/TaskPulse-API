using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Infrastructure.Outbox;

/// <summary>
/// Deletes processed outbox messages after the retention period. Their
/// consumer rows go with them (cascade). Dead-lettered messages are kept.
/// </summary>
[DisallowConcurrentExecution]
public sealed class CleanOutboxMessagesJob(
    TaskPulseDbContext dbContext,
    OutboxOptions options,
    TimeProvider timeProvider,
    ILogger<CleanOutboxMessagesJob> logger) : IJob
{
    // Quartz 4 jobs return ValueTask and receive the cancellation token.
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var cutoffUtc = timeProvider.GetUtcNow().UtcDateTime.AddDays(-options.RetentionDays);

        var deleted = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc != null && message.ProcessedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            logger.LogInformation("Deleted {Count} processed outbox messages older than {Days} days.", deleted, options.RetentionDays);
    }
}