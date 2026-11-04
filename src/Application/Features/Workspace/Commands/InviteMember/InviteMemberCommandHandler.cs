using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.InviteMember;

public class InviteMemberCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<InviteMemberCommand, Result>
{
    public async Task<Result> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAndInvitationsAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null || !workspace.IsMember(request.ActorId))
            return WorkspaceErrors.WorkspaceNotFound;

        var email = request.Email.Trim().ToLowerInvariant();

        var invitedUser = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (invitedUser is null)
            return WorkspaceCommandErrors.InvitedUserNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var inviteResult = workspace.InviteMember(invitedUser.Id, request.ActorId, request.Role, nowUtc);

        if (inviteResult.IsFailure)
            return inviteResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}