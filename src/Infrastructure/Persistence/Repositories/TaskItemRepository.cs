using Domain.Aggregates.Tasks.Task;
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
                .Where(task => task.AssignedTo == assigneeId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TaskItem>> GetByCreatorIdAsync(
            Guid createdBy,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TaskItem>()
                .AsNoTracking()
                .Where(task => task.CreatedBy == createdBy)
                .ToListAsync(cancellationToken);
        }

        public async Task<TaskItem?> GetByIdWithCommentsAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TaskItem>()
                .Include(task => task.Comments)
                .FirstOrDefaultAsync(
                    task => task.Id == taskId,
                    cancellationToken);
        }

        public async Task<List<TaskItem>> GetByWorkspaceIdAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TaskItem>()
                .AsNoTracking()
                .Where(task => task.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken);
        }
    }
}
