using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.RemoveDocumentLink;

public class RemoveDocumentLinkCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveDocumentLinkCommand, Result>
{
    public async Task<Result> Handle(RemoveDocumentLinkCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

        if (task is null || task.IsDeleted)
            return TaskCommandErrors.TaskNotFound;

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(task.WorkspaceId, cancellationToken);

        var role = workspace?.GetMemberRole(request.UserId);

        if (workspace is null || role is null)
            return TaskCommandErrors.TaskNotFound;

        if (workspace.IsArchived)
            return WorkspaceErrors.WorkspaceDeletedOrArchived;

        var canEdit = task.CreatedBy == request.UserId
            || task.AssignedTo == request.UserId
            || TaskPermissions.IsManager(role.Value);

        if (!canEdit)
            return TaskCommandErrors.NotAllowed;

        var removeResult = task.RemoveDocumentLink();

        if (removeResult.IsFailure)
            return removeResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}