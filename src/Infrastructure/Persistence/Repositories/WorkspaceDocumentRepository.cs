using Domain.Aggregates.WorkspaceDocuments;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class WorkspaceDocumentRepository(TaskPulseDbContext dbContext)
    : Repository<WorkspaceDocument>(dbContext), IWorkspaceDocumentRepository
{
    public Task<List<WorkspaceDocument>> ListForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkspaceDocuments.AsNoTracking().Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<WorkspaceDocument?> GetInWorkspaceAsync(Guid workspaceId, Guid documentId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkspaceDocuments.FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Id == documentId, cancellationToken);

    public Task<bool> ExistsAsync(Guid workspaceId, string googleFileId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkspaceDocuments.AnyAsync(x => x.WorkspaceId == workspaceId && x.GoogleFileId == googleFileId, cancellationToken);
}
