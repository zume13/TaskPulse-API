using Application.Abstractions.Auth;
using Application.Constants;
using Domain.Aggregates.PasswordResetToken;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    ISecureTokenService secureTokenService,
    IEmailService emailService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
            return Result.Success();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var oldTokens = await passwordResetTokenRepository.GetUnusedByUserIdAsync(user.Id, cancellationToken);

        foreach (var oldToken in oldTokens)
            oldToken.Consume(nowUtc);

        var rawToken = secureTokenService.GenerateToken();

        var tokenResult = PasswordResetToken.Create(
            secureTokenService.Hash(rawToken),
            user.Id,
            nowUtc.Add(AuthConstants.PasswordResetTokenLifetime),
            nowUtc);

        if (tokenResult.IsFailure)
            return tokenResult.Error;

        await passwordResetTokenRepository.AddAsync(tokenResult.value, cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error;

        try
        {
            await emailService.SendPasswordResetEmailAsync(
                user.Email.Value,
                user.FirstName.Value,
                rawToken,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email for user {UserId}.", user.Id);
        }

        return Result.Success();
    }
}