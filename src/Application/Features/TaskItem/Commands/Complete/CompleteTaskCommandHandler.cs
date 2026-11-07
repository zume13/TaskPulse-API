using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Complete;

public class CompleteTaskCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CompleteTaskCommand, Result>
{
    public async Task<Result> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
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

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var completeResult = task.Complete(request.UserId, nowUtc);

        if (completeResult.IsFailure)
            return completeResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}