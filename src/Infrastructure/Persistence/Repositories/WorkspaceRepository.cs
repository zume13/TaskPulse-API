using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class WorkspaceRepository : Repository<Workspace>, IWorkspaceRepository
{
    public WorkspaceRepository(TaskPulseDbContext dbContext) : base(dbContext) {}

    public async Task<List<Workspace>> GetByMemberIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Workspace>()
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Workspace?> GetByInvitationIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Workspace>()
            .Include(w => w.Invitations)
            .FirstOrDefaultAsync(
                w => w.Invitations.Any(i => i.Id == invitationId),
                cancellationToken);
    }

    public async Task<Workspace?> GetByIdWithMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Workspace>()
            .Include(w => w.Members)
            .FirstOrDefaultAsync(
                w => w.Id == workspaceId,
                cancellationToken);
    }

    public async Task<Workspace?> GetByIdWithInvitationsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Workspace>()
            .Include(w => w.Invitations)
            .FirstOrDefaultAsync(
                w => w.Id == workspaceId,
                cancellationToken);
    }
}
