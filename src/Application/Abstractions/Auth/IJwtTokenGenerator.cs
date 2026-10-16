using Domain.Aggregates.User;

namespace Application.Abstractions.Auth;

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);
}