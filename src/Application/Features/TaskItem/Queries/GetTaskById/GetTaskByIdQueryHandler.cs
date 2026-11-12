using Application.Dto;
using Application.Features.Notifications;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.User;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Queries.GetTaskById;

public class GetTaskByIdQueryHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider) : IRequestHandler<GetTaskByIdQuery, ResultT<TaskDetailsDto>>
{
    public async Task<ResultT<TaskDetailsDto>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdWithCommentsAsync(request.TaskId, cancellationToken);

        if (task is null)
            return TaskCommandErrors.TaskNotFound;

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(task.WorkspaceId, cancellationToken);

        if (workspace is null || !workspace.IsMember(request.UserId))
            return TaskCommandErrors.TaskNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var comments = task.Comments
            .Where(comment => !comment.IsDeleted)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ToList();

        var userIds = new HashSet<Guid>(comments.Select(comment => comment.AuthorId)) { task.CreatedBy };

        if (task.AssignedTo is { } assigneeId)
            userIds.Add(assigneeId);

        var users = (await userRepository.GetByIdsAsync(userIds, cancellationToken))
            .ToDictionary(user => user.Id);

        string NameOf(Guid id) => UserNames.DisplayName(users.GetValueOrDefault(id));

        var commentDtos = comments
            .Select(comment => new CommentDto(
                comment.Id,
                comment.AuthorId,
                NameOf(comment.AuthorId),
                comment.Body.Value,
                comment.CreatedAtUtc,
                comment.UpdatedAtUtc))
            .ToList();

        var linkedDocument = task.LinkedDocument is null
            ? null
            : new DocumentLinkDto(task.LinkedDocument.Url, task.LinkedDocument.Label);

        return new TaskDetailsDto(
            task.Id,
            task.WorkspaceId,
            task.Title.Value,
            task.Description,
            task.DueDate,
            task.GetEffectiveStatus(nowUtc),
            task.TaskPriority,
            task.AssignedTo,
            task.AssignedTo is { } assignee ? NameOf(assignee) : null,
            task.CreatedBy,
            NameOf(task.CreatedBy),
            task.CreatedAtUtc,
            task.CompletedAtUtc,
            linkedDocument,
            commentDtos);
    }
}
