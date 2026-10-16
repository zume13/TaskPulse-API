using Application.Abstractions.Auth;
using Application.Abstractions.Base;
using Application.Abstractions.Repositories;
using Application.Constants;
using Application.Dto;
using Domain.Aggregates.RefreshToken;
using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Refresh;

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RefreshTokenCommand, ResultT<TokenDto>>
{
    public async Task<ResultT<TokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(
            secureTokenService.Hash(request.RefreshToken),
            cancellationToken);

        if (storedToken is null)
            return RefreshErrors.InvalidRefreshToken;

        if (storedToken.RevokedAtUtc is not null)
        {
            var activeTokens = await refreshTokenRepository.GetUnrevokedByUserIdAsync(storedToken.UserId, cancellationToken);

            foreach (var activeToken in activeTokens)
                activeToken.Revoke(nowUtc);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return RefreshErrors.InvalidRefreshToken;
        }

        if (!storedToken.IsActive(nowUtc))
            return RefreshErrors.InvalidRefreshToken;

        var user = await userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user is null)
            return RefreshErrors.InvalidRefreshToken;

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);

        var rawRefreshToken = secureTokenService.GenerateToken();
        var refreshTokenExpiresAtUtc = nowUtc.Add(AuthConstants.RefreshTokenLifetime);

        var newTokenResult = RefreshToken.Create(
            secureTokenService.Hash(rawRefreshToken),
            user.Id,
            refreshTokenExpiresAtUtc,
            nowUtc);

        if (newTokenResult.IsFailure)
            return newTokenResult.Error;

        var newToken = newTokenResult.value;

        var revokeResult = storedToken.Revoke(nowUtc, newToken.Id);

        if (revokeResult.IsFailure)
            return revokeResult.Error;

        await refreshTokenRepository.AddAsync(newToken, cancellationToken);

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