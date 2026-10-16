using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Login;

public static class LoginErrors
{
    public static Error InvalidCredentials => Error.Failure("Login.InvalidCredentials", "Email or password is incorrect.");

    public static Error EmailNotVerified => Error.Failure("Login.EmailNotVerified", "Please verify your email before logging in.");
}