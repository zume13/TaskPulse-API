using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using Domain.Shared.ValueObjects;
using MediatR;

namespace Application.Features.Tasks.Commands.Update;

public class UpdateTaskCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<UpdateTaskCommand, Result>
{
    public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
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

        if (task.CreatedBy != request.UserId && !TaskPermissions.IsManager(role.Value))
            return TaskCommandErrors.NotAllowed;

        var titleResult = Name.Create(request.Title);

        if (titleResult.IsFailure)
            return titleResult.Error;

        var dueDateUtc = request.DueDate.UtcDateTime;

        var detailsChanged = titleResult.value.Value != task.Title.Value || dueDateUtc != task.DueDate;

        if (detailsChanged)
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            var detailsResult = task.UpdateDetails(titleResult.value, dueDateUtc, nowUtc);

            if (detailsResult.IsFailure)
                return detailsResult.Error;
        }

        var priorityResult = task.UpdatePriority(request.Priority);

        if (priorityResult.IsFailure)
            return priorityResult.Error;

        var descriptionResult = task.UpdateDescription(request.Description);

        if (descriptionResult.IsFailure)
            return descriptionResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}