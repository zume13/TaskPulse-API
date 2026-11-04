using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.AcceptInvitation;

public class AcceptInvitationCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<AcceptInvitationCommand, Result>
{
    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByInvitationIdAsync(request.InvitationId, cancellationToken);

        var isInvitedUser = workspace is not null && workspace.Invitations.Any(
            invitation => invitation.Id == request.InvitationId
                && invitation.InvitedUserId == request.UserId);

        if (workspace is null || !isInvitedUser)
            return WorkspaceErrors.InvitationNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var acceptResult = workspace.AcceptInvitation(request.InvitationId, request.UserId, nowUtc);

        if (acceptResult.IsFailure)
            return acceptResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}