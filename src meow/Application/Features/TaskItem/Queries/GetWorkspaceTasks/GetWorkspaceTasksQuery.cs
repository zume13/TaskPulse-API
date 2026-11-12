using Application.Dto;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.Task.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Queries.GetWorkspaceTasks;

public record GetWorkspaceTasksQuery(
    Guid WorkspaceId,
    Guid UserId,
    Status? Status,
    Priority? Priority,
    Guid? AssignedTo,
    string? Search,
    TaskSortBy SortBy,
    bool Descending,
    int Page,
    int PageSize) : IRequest<ResultT<PagedResultDto<TaskDto>>>;
