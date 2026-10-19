using Domain.Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Login;

public static class LoginErrors
{
    public static Error InvalidCredentials => Error.Unauthorized("Login.InvalidCredentials", "Email or password is incorrect.");

    public static Error EmailNotVerified => Error.Forbidden("Login.EmailNotVerified", "Please verify your email before logging in.");
}