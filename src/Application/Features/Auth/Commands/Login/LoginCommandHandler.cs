using Application.Abstractions.Auth;
using Application.Abstractions.Base;
using Application.Abstractions.Repositories;
using Application.Constants;
using Application.Dto;
using Domain.Aggregates.RefreshToken;
using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    ISecureTokenService secureTokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, ResultT<TokenDto>>
{
    public async Task<ResultT<TokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash.Value))
            return LoginErrors.InvalidCredentials;

        if (!user.IsEmailVerified)
            return LoginErrors.EmailNotVerified;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);

        var rawRefreshToken = secureTokenService.GenerateToken();
        var refreshTokenExpiresAtUtc = nowUtc.Add(AuthConstants.RefreshTokenLifetime);

        var refreshTokenResult = RefreshToken.Create(
            secureTokenService.Hash(rawRefreshToken),
            user.Id,
            refreshTokenExpiresAtUtc,
            nowUtc);

        if (refreshTokenResult.IsFailure)
            return refreshTokenResult.Error;

        await refreshTokenRepository.AddAsync(refreshTokenResult.value, cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error;

        return new TokenDto(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresAtUtc,
            refreshTokenExpiresAtUtc);
    }
}