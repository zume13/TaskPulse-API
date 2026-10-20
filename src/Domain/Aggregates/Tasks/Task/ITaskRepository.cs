
using Domain.Shared.Base;

namespace Domain.Aggregates.Tasks.Task
{
    public interface ITaskRepository : IRepository<Task>
    {
        Task<List<Task>> GetByWorkspaceIdAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default);

        Task<List<Task>> GetByAssigneeIdAsync(
            Guid assigneeId,
            CancellationToken cancellationToken = default);

        Task<List<Task>> GetByCreatorIdAsync(
            Guid createdBy,
            CancellationToken cancellationToken = default);

        Task<Task?> GetByIdWithCommentsAsync(
            Guid taskId,
            CancellationToken cancellationToken = default);
    }
}
