using Domain.Aggregates.Tasks.Task.Enums;

namespace Domain.Aggregates.Tasks.Task;

public enum TaskSortBy
{
    DueDate = 0,
    CreatedAt = 1,
    Priority = 2,
    Title = 3
}

/// <summary>
/// Filters, sorting and paging for a task list. StatusFilter uses the
/// effective status: Overdue is not stored, so it is worked out from the
/// due date and NowUtc.
/// </summary>
public sealed record TaskSearchCriteria(
    Guid? WorkspaceId,
    Guid? AssigneeId,
    Status? StatusFilter,
    Priority? PriorityFilter,
    string? SearchText,
    TaskSortBy SortBy,
    bool Descending,
    int Skip,
    int Take,
    DateTime NowUtc);

public sealed record TaskSearchResult(IReadOnlyList<TaskItem> Items, int TotalCount);
