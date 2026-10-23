using Domain.Shared.ResultPattern;

namespace Application.Features.Users.Commands.ChangeEmail;

public static class ChangeEmailErrors
{
    public static Error InvalidPassword => Error.Failure("ChangeEmail.InvalidPassword", "The current password is incorrect.");

    public static Error EmailAlreadyInUse => Error.Conflict("ChangeEmail.EmailAlreadyInUse", "An account with this email already exists.");
}