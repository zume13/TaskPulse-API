
using Domain.Shared.Base;

namespace Domain.Aggregates.Tasks.Task
{
    public interface ITaskItemRepository : IRepository<TaskItem>
    {
        Task<List<TaskItem>> GetByWorkspaceIdAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default);

        Task<List<TaskItem>> GetByAssigneeIdAsync(
            Guid assigneeId,
            CancellationToken cancellationToken = default);

        Task<List<TaskItem>> GetByCreatorIdAsync(
            Guid createdBy,
            CancellationToken cancellationToken = default);

        Task<TaskItem?> GetByIdWithCommentsAsync(
            Guid taskId,
            CancellationToken cancellationToken = default);

        /// <summary>Filtered, sorted and paged list of non-deleted tasks, read-only.</summary>
        Task<TaskSearchResult> SearchAsync(
            TaskSearchCriteria criteria,
            CancellationToken cancellationToken = default);
    }
}
