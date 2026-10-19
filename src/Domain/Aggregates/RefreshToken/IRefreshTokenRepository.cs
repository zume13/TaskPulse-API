using Domain.Shared.Base;

namespace Domain.Aggregates.RefreshToken;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<List<RefreshToken>> GetUnrevokedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}