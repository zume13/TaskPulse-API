using Domain.Shared.ResultPattern;

namespace Application.Features.Auth.Commands.ResetPassword;

public static class ResetPasswordErrors
{
    public static Error InvalidToken => Error.Failure("ResetPassword.InvalidToken", "The password reset link is invalid or has expired.");
}