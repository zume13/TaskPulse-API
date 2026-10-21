using Domain.Aggregates.RefreshToken;
using Domain.Shared.ResultPattern;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository
    : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(TaskPulseDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<List<RefreshToken>> GetUnrevokedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetActiveByTokenHashAsync(
        string tokenHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(
                token => token.TokenHash == tokenHash &&
                         token.RevokedAtUtc == null &&
                         token.ExpiresAtUtc > nowUtc,
                cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .AnyAsync(
                token => token.UserId == userId &&
                         token.RevokedAtUtc == null &&
                         token.ExpiresAtUtc > nowUtc,
                cancellationToken);
    }

    public async Task<int> RevokeAllByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.Set<RefreshToken>()
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var revokedCount = 0;

        foreach (var token in tokens)
        {
            Result result = token.Revoke(nowUtc);

            if (result.IsSuccess)
                revokedCount++;
        }

        return revokedCount;
    }

    public async Task<List<RefreshToken>> GetExpiredAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .Where(token => token.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByReplacedByTokenIdAsync(
        Guid replacedByTokenId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(
                token => token.ReplacedByTokenId == replacedByTokenId,
                cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .Where(token => token.ExpiresAtUtc <= nowUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}