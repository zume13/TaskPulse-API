using Domain.Aggregates.PasswordResetToken.Errors;
using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.PasswordResetToken;

public class PasswordResetToken : AggregateRoot
{
    private PasswordResetToken() { }

    private PasswordResetToken(Guid id, string tokenHash, Guid userId, DateTime createdAtUtc, DateTime expiresAtUtc) : base(id)
    {
        TokenHash = tokenHash;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string TokenHash { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    public static ResultT<PasswordResetToken> Create(string tokenHash, Guid userId, DateTime expiresAtUtc, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            return PasswordResetTokenErrors.TokenHashEmpty;

        if (userId == Guid.Empty)
            return PasswordResetTokenErrors.UserIdEmpty;

        if (expiresAtUtc <= nowUtc)
            return PasswordResetTokenErrors.AlreadyExpired;

        return new PasswordResetToken(Guid.NewGuid(), tokenHash, userId, nowUtc, expiresAtUtc);
    }

    public Result Consume(DateTime nowUtc)
    {
        if (UsedAtUtc is not null)
            return PasswordResetTokenErrors.AlreadyUsed;

        UsedAtUtc = nowUtc;

        return Result.Success();
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsUsable(DateTime nowUtc) => UsedAtUtc is null && !IsExpired(nowUtc);
}