using Application.Abstractions.Auth;
using Domain.Shared.Base;
using Domain.Aggregates.RefreshToken;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(
            secureTokenService.Hash(request.RefreshToken),
            cancellationToken);

        if (storedToken is null || storedToken.RevokedAtUtc is not null)
            return Result.Success();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var revokeResult = storedToken.Revoke(nowUtc);

        if (revokeResult.IsFailure)
            return revokeResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}