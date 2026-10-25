using Application.Abstractions.Auth;
using Domain.Aggregates.PasswordResetToken;
using Domain.Aggregates.RefreshToken;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ISecureTokenService secureTokenService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var resetToken = await passwordResetTokenRepository.GetByTokenHashAsync(
            secureTokenService.Hash(request.Token),
            cancellationToken);

        if (resetToken is null || !resetToken.IsUsable(nowUtc))
            return ResetPasswordErrors.InvalidToken;

        var user = await userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);

        if (user is null)
            return ResetPasswordErrors.InvalidToken;

        var changeResult = user.ChangePassword(passwordHasher.Hash(request.NewPassword));

        if (changeResult.IsFailure)
            return changeResult.Error;

        var consumeResult = resetToken.Consume(nowUtc);

        if (consumeResult.IsFailure)
            return ResetPasswordErrors.InvalidToken;

        var activeTokens = await refreshTokenRepository.GetUnrevokedByUserIdAsync(user.Id, cancellationToken);

        foreach (var activeToken in activeTokens)
            activeToken.Revoke(nowUtc);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}