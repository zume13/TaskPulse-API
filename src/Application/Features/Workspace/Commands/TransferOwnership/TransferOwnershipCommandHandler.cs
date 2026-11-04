using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.TransferOwnership;

public class TransferOwnershipCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<TransferOwnershipCommand, Result>
{
    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);

        if (workspace is null || !workspace.IsMember(request.ActorId))
            return WorkspaceErrors.WorkspaceNotFound;

        var transferResult = workspace.TransferOwnership(request.ActorId, request.NewOwnerId);

        if (transferResult.IsFailure)
            return transferResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}