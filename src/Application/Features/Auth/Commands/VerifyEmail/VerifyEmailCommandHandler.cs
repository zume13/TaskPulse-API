using Application.Abstractions.Base;
using Application.Abstractions.Repositories;
using Domain.Aggregates.User.Errors;
using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return UserErrors.InvalidVerificationToken;

        var verifyResult = user.VerifyEmail(request.Token);

        if (verifyResult.IsFailure)
            return verifyResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}