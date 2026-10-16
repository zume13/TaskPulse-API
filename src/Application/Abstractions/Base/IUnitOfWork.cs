using Shared.ResultPattern;

namespace Application.Abstractions.Base;

public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}