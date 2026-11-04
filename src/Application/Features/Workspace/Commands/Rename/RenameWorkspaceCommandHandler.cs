using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.Rename;

public class RenameWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RenameWorkspaceCommand, Result>
{
    public async Task<Result> Handle(RenameWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);

        if (workspace is null || !workspace.IsMember(request.ActorId))
            return WorkspaceErrors.WorkspaceNotFound;

        var renameResult = workspace.Rename(request.ActorId, request.Name);

        if (renameResult.IsFailure)
            return renameResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}