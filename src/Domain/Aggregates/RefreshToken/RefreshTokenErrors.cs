using Shared.ResultPattern;

namespace Domain.Aggregates.RefreshToken;

public static class RefreshTokenErrors
{
    public static Error TokenHashEmpty => Error.Validation("RefreshToken.TokenHashEmpty", "The token hash cannot be empty.");
    public static Error UserIdEmpty => Error.Validation("RefreshToken.UserIdEmpty", "The token must belong to a user.");
    public static Error AlreadyExpired => Error.Validation("RefreshToken.AlreadyExpired", "The token expiry date must be in the future.");
    public static Error AlreadyRevoked => Error.Conflict("RefreshToken.AlreadyRevoked", "The token has already been revoked.");
}