using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.Task.Enums;

namespace Application.Dto;

/// <summary>Query string of the task list endpoints, e.g. ?status=Overdue&amp;sortBy=DueDate&amp;page=2.</summary>
public record TaskListRequestDto(
    Status? Status = null,
    Priority? Priority = null,
    Guid? AssignedTo = null,
    string? Search = null,
    TaskSortBy SortBy = TaskSortBy.DueDate,
    bool Descending = false,
    int Page = 1,
    int PageSize = 20);
