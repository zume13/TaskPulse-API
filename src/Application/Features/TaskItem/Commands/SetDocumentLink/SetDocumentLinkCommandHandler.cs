using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.SetDocumentLink;

public class SetDocumentLinkCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SetDocumentLinkCommand, Result>
{
    public async Task<Result> Handle(SetDocumentLinkCommand request, CancellationToken cancellationToken)
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

        var linkResult = DocumentLink.Create(request.Url, request.Label);

        if (linkResult.IsFailure)
            return linkResult.Error;

        var setResult = task.UpdateDocumentLink(linkResult.value);

        if (setResult.IsFailure)
            return setResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}