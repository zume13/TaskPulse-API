using Application.Abstractions.Events;
using Domain.Shared.Base;
using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Database;

public sealed class UnitOfWork(
    TaskPulseDbContext dbContext,
    IDomainEventDispatcher domainEventDispatcher,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Error.Problem("Database.UpdateFailed", "The database update failed.");
        }

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents.ToList())
                aggregate.RemoveDomainEvent(domainEvent);
        }

        try
        {
            await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Dispatching domain events failed after the changes were saved.");
        }

        return Result.Success();
    }
}