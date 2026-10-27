using Domain.Shared.ResultPattern;

namespace Application.Features.Users.Commands;

public static class UserCommandErrors
{
    public static class ChangeEmail
    {
        public static Error InvalidPassword => Error.Failure("ChangeEmail.InvalidPassword", "The current password is incorrect.");
        public static Error EmailAlreadyInUse => Error.Conflict("ChangeEmail.EmailAlreadyInUse", "An account with this email already exists.");
    }
}