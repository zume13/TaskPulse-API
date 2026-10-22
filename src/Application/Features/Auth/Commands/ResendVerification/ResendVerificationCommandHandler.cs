using Application.Abstractions.Auth;
using Application.Abstractions.Base;
using Domain.Aggregates.User;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.ResendVerification;

public class ResendVerificationCommandHandler(
    IUserRepository userRepository,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<ResendVerificationCommand, Result>
{
    public async Task<Result> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || user.IsEmailVerified)
            return Result.Success();

        var refreshResult = user.RefreshVerificationToken(secureTokenService.GenerateToken());

        if (refreshResult.IsFailure)
            return refreshResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}