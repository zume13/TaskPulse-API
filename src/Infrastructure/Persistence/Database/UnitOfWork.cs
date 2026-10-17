using Application.Abstractions.Base;
using Microsoft.EntityFrameworkCore;
using Shared.ResultPattern;

namespace Infrastructure.Persistence.Database;

public sealed class UnitOfWork(TaskPulseDbContext dbContext) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Error.Failure("Database.UpdateFailed", "The database update failed.");
        }
    }
}