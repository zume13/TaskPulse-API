using Application.Abstractions.Auth;
using Application.Abstractions.Base;
using Application.Abstractions.Repositories;
using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash.Value))
            return ChangePasswordErrors.InvalidCurrentPassword;

        var changeResult = user.ChangePassword(passwordHasher.Hash(request.NewPassword));

        if (changeResult.IsFailure)
            return changeResult.Error;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var activeTokens = await refreshTokenRepository.GetUnrevokedByUserIdAsync(user.Id, cancellationToken);

        foreach (var activeToken in activeTokens)
            activeToken.Revoke(nowUtc);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}