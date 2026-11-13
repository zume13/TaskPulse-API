using Domain.Aggregates.Tasks;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.Task.Enums;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class TaskItemRepository : Repository<TaskItem>, ITaskItemRepository
    {
        public TaskItemRepository(TaskPulseDbContext dbContext)
            : base(dbContext)
        {
        }

        public async Task<List<TaskItem>> GetByAssigneeIdAsync(
        Guid assigneeId,
        CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TaskItem>()
                .AsNoTracking()
                .Where(task => task.AssignedTo == assigneeId && !task.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TaskItem>> GetByCreatorIdAsync(
            Guid createdBy,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TaskItem>()
                .AsNoTracking()
                .Where(task => task.CreatedBy == createdBy && !task.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<TaskItem?> GetByIdWithCommentsAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TaskItem>()
                .Include(task => task.Comments)
                .FirstOrDefaultAsync(
                    task => task.Id == taskId && !task.IsDeleted,
                    cancellationToken);
        }

        public async Task<List<TaskItem>> GetByWorkspaceIdAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TaskItem>()
                .AsNoTracking()
                .Where(task => task.WorkspaceId == workspaceId && !task.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<TaskSearchResult> SearchAsync(
            TaskSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Set<TaskItem>()
                .AsNoTracking()
                .Where(task => !task.IsDeleted);

            if (criteria.WorkspaceId is { } workspaceId)
                query = query.Where(task => task.WorkspaceId == workspaceId);

            if (criteria.AssigneeId is { } assigneeId)
                query = query.Where(task => task.AssignedTo == assigneeId);

            if (criteria.PriorityFilter is { } priority)
                query = query.Where(task => task.TaskPriority == priority);

            if (criteria.StatusFilter is { } status)
            {
                var now = criteria.NowUtc;

                // Overdue is never stored (see TaskItem.GetEffectiveStatus), so the
                // filter is done on the due date. Pending/Ongoing means "not yet overdue".
                query = status switch
                {
                    Status.Overdue => query.Where(task =>
                        (task.Status == Status.Pending || task.Status == Status.Ongoing)
                        && task.DueDate < now),

                    Status.Pending or Status.Ongoing => query.Where(task =>
                        task.Status == status && task.DueDate >= now),

                    _ => query.Where(task => task.Status == status)
                };
            }

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var escaped = criteria.SearchText.Trim()
                    .Replace("\\", "\\\\")
                    .Replace("%", "\\%")
                    .Replace("_", "\\_");

                var pattern = $"%{escaped}%";

                query = query.Where(task =>
                    EF.Functions.ILike(task.Title.Value, pattern)
                    || (task.Description != null && EF.Functions.ILike(task.Description, pattern)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var ordered = (criteria.SortBy, criteria.Descending) switch
            {
                (TaskSortBy.CreatedAt, false) => query.OrderBy(task => task.CreatedAtUtc),
                (TaskSortBy.CreatedAt, true) => query.OrderByDescending(task => task.CreatedAtUtc),
                (TaskSortBy.Priority, false) => query.OrderBy(task => task.TaskPriority),
                (TaskSortBy.Priority, true) => query.OrderByDescending(task => task.TaskPriority),
                (TaskSortBy.Title, false) => query.OrderBy(task => task.Title.Value),
                (TaskSortBy.Title, true) => query.OrderByDescending(task => task.Title.Value),
                (_, true) => query.OrderByDescending(task => task.DueDate),
                _ => query.OrderBy(task => task.DueDate)
            };

            var items = await ordered
                .ThenBy(task => task.Id)
                .Skip(Math.Max(0, criteria.Skip))
                .Take(Math.Clamp(criteria.Take, 1, 100))
                .ToListAsync(cancellationToken);

            return new TaskSearchResult(items, totalCount);
        }
    }
}