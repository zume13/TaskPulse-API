using Domain.Shared.Base;

namespace Domain.Aggregates.PasswordResetToken;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<List<PasswordResetToken>> GetUnusedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}