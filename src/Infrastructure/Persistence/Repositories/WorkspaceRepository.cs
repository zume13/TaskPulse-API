using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class WorkspaceRepository : Repository<Workspace>, IWorkspaceRepository
{
    public WorkspaceRepository(TaskPulseDbContext dbContext) : base(dbContext) { }

    public async Task<WorkspaceSearchResult> GetAllWithMembersAsync(
        bool includeArchived,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Workspace>().AsNoTracking();

        if (!includeArchived)
            query = query.Where(w => !w.IsArchived);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(w => w.Members)
            .AsSplitQuery()
            .OrderByDescending(w => w.CreatedAtUtc)
            .ThenBy(w => w.Id)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return new WorkspaceSearchResult(items, totalCount);
    }

    public async Task<List<UserWorkspaceMembership>> GetMembershipsByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return [];

        var query = _dbContext.Set<Workspace>().AsNoTracking();

        if (!includeArchived)
            query = query.Where(w => !w.IsArchived);

        return await query
            .SelectMany(
                w => w.Members.Where(m => userIds.Contains(m.UserId)),
                (w, m) => new UserWorkspaceMembership(
                    m.UserId,
                    w.Id,
                    w.WorkspaceName.Value,
                    w.IsArchived,
                    m.Role,
                    m.JoinedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Workspace>> GetByMemberIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Workspace>()
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Workspace>> GetByMemberIdWithMembersAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Workspace>()
            .AsNoTracking()
            .Include(w => w.Members)
            .Where(w => w.Members.Any(m => m.UserId == userId));

        if (!includeArchived)
            query = query.Where(w => !w.IsArchived);

        return await query
            .OrderByDescending(w => w.CreatedAtUtc)
            .ThenBy(w => w.Id)
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
    public async Task<Workspace?> GetByIdWithMembersAndInvitationsAsync(
    Guid workspaceId,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Workspace>()
            .Include(w => w.Members)
            .Include(w => w.Invitations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                w => w.Id == workspaceId,
                cancellationToken);
    }
}
