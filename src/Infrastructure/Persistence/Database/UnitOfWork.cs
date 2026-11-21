using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domain.Aggregates.Tasks.TaskDocument;
using Infrastructure.Persistence.EntityConfiguration;
using Npgsql;

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
        catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(x => x.Entity is TaskDocument))
        {
            return TaskDocumentCollectionErrors.Changed;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: TaskDocumentConfig.UniqueUrlIndex })
        {
            return TaskDocumentCollectionErrors.Duplicate;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation } postgres
            && postgres.ConstraintName == TaskDocumentConfig.LegacySlotIndex)
        {
            return TaskDocumentCollectionErrors.Changed;
        }
        catch (DbUpdateException exception) when (exception.Entries.Any(x => x.Entity is TaskDocument))
        {
            logger.LogError("Saving task document changes failed; database details withheld to protect document URLs.");
            return Error.Problem("Database.UpdateFailed", "The database update failed.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Saving changes failed.");

            return Error.Problem("Database.UpdateFailed", "The database update failed.");
        }
    }
}
