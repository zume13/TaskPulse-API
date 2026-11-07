using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Unassign;

public class UnassignTaskCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UnassignTaskCommand, Result>
{
    public async Task<Result> Handle(UnassignTaskCommand request, CancellationToken cancellationToken)
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

        if (task.AssignedTo != request.UserId && !TaskPermissions.IsManager(role.Value))
            return TaskCommandErrors.NotAllowed;

        var unassignResult = task.Unassign(request.UserId);

        if (unassignResult.IsFailure)
            return unassignResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}