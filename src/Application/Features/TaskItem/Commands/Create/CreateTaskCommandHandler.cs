using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using Domain.Shared.ValueObjects;
using MediatR;

namespace Application.Features.Tasks.Commands.Create;

public class CreateTaskCommandHandler(
    IWorkspaceRepository workspaceRepository,
    ITaskItemRepository taskRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTaskCommand, ResultT<TaskDto>>
{
    public async Task<ResultT<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);

        if (workspace is null || !workspace.IsMember(request.UserId))
            return WorkspaceErrors.WorkspaceNotFound;

        if (workspace.IsArchived)
            return WorkspaceErrors.WorkspaceDeletedOrArchived;

        var titleResult = Name.Create(request.Title);

        if (titleResult.IsFailure)
            return titleResult.Error;

        var taskResult = TaskItem.Create(
            request.WorkspaceId,
            titleResult.value,
            request.DueDate.UtcDateTime,
            request.UserId,
            request.Priority,
            request.Description);

        if (taskResult.IsFailure)
            return taskResult.Error;

        var task = taskResult.value;

        await taskRepository.AddAsync(task, cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error;

        return new TaskDto(
            task.Id,
            task.WorkspaceId,
            task.Title.Value,
            task.Description,
            task.DueDate,
            task.Status,
            task.TaskPriority,
            task.AssignedTo,
            task.CreatedBy,
            task.CreatedAtUtc);
    }
}