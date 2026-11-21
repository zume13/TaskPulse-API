using Domain.Shared.Base;

namespace Domain.Aggregates.WorkspaceDocuments;

public interface IWorkspaceDocumentRepository : IRepository<WorkspaceDocument>
{
    Task<List<WorkspaceDocument>> ListForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<WorkspaceDocument?> GetInWorkspaceAsync(Guid workspaceId, Guid documentId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid workspaceId, string googleFileId, CancellationToken cancellationToken = default);
}
