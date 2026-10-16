using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.ChangePassword;

public static class ChangePasswordErrors
{
    public static Error InvalidCurrentPassword => Error.Failure("ChangePassword.InvalidCurrentPassword", "The current password is incorrect.");
}