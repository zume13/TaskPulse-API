using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.PasswordResetToken.Errors;

public static class PasswordResetTokenErrors
{
    public static Error TokenHashEmpty => Error.Validation("PasswordResetToken.TokenHashEmpty", "The token hash cannot be empty.");
    public static Error UserIdEmpty => Error.Validation("PasswordResetToken.UserIdEmpty", "The token must belong to a user.");
    public static Error AlreadyExpired => Error.Validation("PasswordResetToken.AlreadyExpired", "The token expiry date must be in the future.");
    public static Error AlreadyUsed => Error.Conflict("PasswordResetToken.AlreadyUsed", "The token has already been used.");
}