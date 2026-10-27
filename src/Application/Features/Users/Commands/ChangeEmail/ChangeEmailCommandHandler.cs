using Application.Abstractions.Auth;
using Application.Features.Auth;
using Domain.Aggregates.RefreshToken;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Users.Commands.ChangeEmail;

public class ChangeEmailCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ChangeEmailCommand, Result>
{
    public async Task<Result> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash.Value))
            return UserCommandErrors.ChangeEmail.InvalidPassword;

        var newEmail = request.NewEmail.Trim().ToLowerInvariant();

        if (newEmail != user.Email.Value && await userRepository.ExistsByEmailAsync(newEmail, cancellationToken))
            return UserCommandErrors.ChangeEmail.EmailAlreadyInUse;

        var changeResult = user.ChangeEmail(newEmail, secureTokenService.GenerateToken());

        if (changeResult.IsFailure)
            return changeResult.Error;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var activeTokens = await refreshTokenRepository.GetUnrevokedByUserIdAsync(user.Id, cancellationToken);

        foreach (var activeToken in activeTokens)
            activeToken.Revoke(nowUtc);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}