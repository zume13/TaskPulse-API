using Application.Dto;
using Domain.Aggregates.Tasks;

namespace Application.Features.Tasks;

internal static class TaskMappings
{
    /// <summary>Status is the effective one, so a late task shows as Overdue.</summary>
    public static TaskDto ToDto(this TaskItem task, DateTime nowUtc) => new(
        task.Id,
        task.WorkspaceId,
        task.Title.Value,
        task.Description,
        task.DueDate,
        task.GetEffectiveStatus(nowUtc),
        task.TaskPriority,
        task.AssignedTo,
        task.CreatedBy,
        task.CreatedAtUtc);
}
