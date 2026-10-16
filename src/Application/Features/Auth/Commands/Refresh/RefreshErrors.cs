using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Refresh;

public static class RefreshErrors
{
    public static Error InvalidRefreshToken => Error.Failure("Refresh.InvalidRefreshToken", "The refresh token is invalid or has expired.");
}