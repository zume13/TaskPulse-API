using Domain.Shared.Base;
using Domain.Aggregates.RefreshToken;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.LogoutAll;

public class LogoutAllCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<LogoutAllCommand, Result>
{
    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var activeTokens = await refreshTokenRepository.GetUnrevokedByUserIdAsync(request.UserId, cancellationToken);

        foreach (var activeToken in activeTokens)
            activeToken.Revoke(nowUtc);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}