using Application.Abstractions.Base;
using Domain.Aggregates.RefreshToken;

namespace Application.Abstractions.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<List<RefreshToken>> GetUnrevokedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}