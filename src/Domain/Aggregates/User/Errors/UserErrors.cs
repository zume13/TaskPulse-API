
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.User.Errors;

public static class UserErrors
{
    public static Error EmailAlreadyVerified => Error.Conflict("User.EmailAlreadyVerified", "The email has already been verified.");
    public static Error InvalidVerificationToken => Error.Validation("User.InvalidVerificationToken", "The verification token is invalid.");
    public static Error InvalidRole => Error.Validation("User.InvalidRole", "The role is invalid.");
    public static Error AlreadyInRole => Error.Conflict("User.AlreadyInRole", "The user already has this role.");
}