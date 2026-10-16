using SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Aggregates.RefreshToken;

public class RefreshToken : AggregateRoot
{
    private RefreshToken() { }

    private RefreshToken(Guid id, string tokenHash, Guid userId, DateTime createdAtUtc, DateTime expiresAtUtc) : base(id)
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
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public static ResultT<RefreshToken> Create(string tokenHash, Guid userId, DateTime expiresAtUtc, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            return RefreshTokenErrors.TokenHashEmpty;

        if (userId == Guid.Empty)
            return RefreshTokenErrors.UserIdEmpty;

        if (expiresAtUtc <= nowUtc)
            return RefreshTokenErrors.AlreadyExpired;

        return new RefreshToken(Guid.NewGuid(), tokenHash, userId, nowUtc, expiresAtUtc);
    }

    public Result Revoke(DateTime nowUtc, Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null)
            return RefreshTokenErrors.AlreadyRevoked;

        RevokedAtUtc = nowUtc;
        ReplacedByTokenId = replacedByTokenId;

        return Result.Success();
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && !IsExpired(nowUtc);
}