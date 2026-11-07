using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Assign;

public class AssignTaskCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AssignTaskCommand, Result>
{
    public async Task<Result> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
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

        if (!workspace.IsMember(request.AssigneeId))
            return TaskCommandErrors.AssigneeNotMember;

        var isManager = TaskPermissions.IsManager(role.Value);

        var isTakingFreeTask = request.AssigneeId == request.UserId && task.AssignedTo is null;

        if (!isManager && !isTakingFreeTask)
            return TaskCommandErrors.NotAllowed;

        var assignResult = task.AssignTo(request.AssigneeId, request.UserId);

        if (assignResult.IsFailure)
            return assignResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}