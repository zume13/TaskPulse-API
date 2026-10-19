using Application.Abstractions.Base;
using Domain.Aggregates.User;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.ChangeRole;

public class ChangeRoleCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        var changeResult = user.ChangeRole(request.NewRole);

        if (changeResult.IsFailure)
            return changeResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}