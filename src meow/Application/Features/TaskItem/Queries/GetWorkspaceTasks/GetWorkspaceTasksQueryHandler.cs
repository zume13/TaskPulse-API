using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Queries.GetWorkspaceTasks;

public class GetWorkspaceTasksQueryHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    TimeProvider timeProvider) : IRequestHandler<GetWorkspaceTasksQuery, ResultT<PagedResultDto<TaskDto>>>
{
    public async Task<ResultT<PagedResultDto<TaskDto>>> Handle(
        GetWorkspaceTasksQuery request,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);

        // Same answer for "does not exist" and "not a member", so ids cannot be probed.
        if (workspace is null || !workspace.IsMember(request.UserId))
            return WorkspaceErrors.WorkspaceNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var criteria = new TaskSearchCriteria(
            request.WorkspaceId,
            request.AssignedTo,
            request.Status,
            request.Priority,
            request.Search?.Trim(),
            request.SortBy,
            request.Descending,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            nowUtc);

        var result = await taskRepository.SearchAsync(criteria, cancellationToken);

        var items = result.Items.Select(task => task.ToDto(nowUtc)).ToList();

        return new PagedResultDto<TaskDto>(items, request.Page, request.PageSize, result.TotalCount);
    }
}
