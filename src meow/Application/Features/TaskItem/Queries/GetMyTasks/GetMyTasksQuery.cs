using Application.Dto;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.Task.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Queries.GetMyTasks;

/// <summary>Tasks assigned to the user, across all of their workspaces.</summary>
public record GetMyTasksQuery(
    Guid UserId,
    Status? Status,
    Priority? Priority,
    string? Search,
    TaskSortBy SortBy,
    bool Descending,
    int Page,
    int PageSize) : IRequest<ResultT<PagedResultDto<TaskDto>>>;
