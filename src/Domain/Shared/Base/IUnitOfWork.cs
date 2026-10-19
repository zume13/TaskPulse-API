
using Domain.Shared.ResultPattern;

namespace Domain.Shared.Base;

public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}