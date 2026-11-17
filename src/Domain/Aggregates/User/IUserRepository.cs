using Domain.Shared.Base;

namespace Domain.Aggregates.User;

public sealed record UserSearchResult(IReadOnlyList<User> Items, int TotalCount);

public interface IUserRepository : IRepository<User>
{
    Task<UserSearchResult> GetPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetByVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<List<User>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}