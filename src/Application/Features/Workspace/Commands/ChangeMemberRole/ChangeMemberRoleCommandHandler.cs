using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.ChangeMemberRole;

public class ChangeMemberRoleCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeMemberRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);

        if (workspace is null || !workspace.IsMember(request.ActorId))
            return WorkspaceErrors.WorkspaceNotFound;

        var changeResult = workspace.ChangeMemberRole(request.ActorId, request.MemberId, request.NewRole);

        if (changeResult.IsFailure)
            return changeResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}