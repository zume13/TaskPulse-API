using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Database;

/// <summary>
/// Saves the aggregate changes. Domain events are stored in the outbox by
/// TaskPulseDbContext in the same transaction, and processed in the
/// background by the Quartz job, so there is nothing to dispatch here.
/// </summary>
public sealed class UnitOfWork(
    TaskPulseDbContext dbContext,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Saving changes failed.");

            return Error.Problem("Database.UpdateFailed", "The database update failed.");
        }
    }
}
