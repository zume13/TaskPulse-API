using Domain.Aggregates.PasswordResetToken;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository
    : Repository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(TaskPulseDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PasswordResetToken>()
            .FirstOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<List<PasswordResetToken>> GetUnusedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PasswordResetToken>()
            .Where(token =>
                token.UserId == userId &&
                token.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
    }
}