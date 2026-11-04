using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.Leave;

public class LeaveWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<LeaveWorkspaceCommand, Result>
{
    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAndInvitationsAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null || !workspace.IsMember(request.UserId))
            return WorkspaceErrors.WorkspaceNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var leaveResult = workspace.Leave(request.UserId, nowUtc);

        if (leaveResult.IsFailure)
            return leaveResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}