using Domain.Shared.ResultPattern;

namespace Application.Features.Auth;

public static class AuthErrors
{
    public static Error UserNotFound => Error.NotFound("Auth.UserNotFound", "The user was not found.");
}