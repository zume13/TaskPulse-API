using Domain.Aggregates.WorkspaceDocuments;

namespace Application.Dto;

public record WorkspaceDocumentDto(Guid Id, Guid WorkspaceId, string Title, string GoogleFileId,
    WorkspaceDocumentSource SourceType, string? MimeType, Guid CreatedByUserId, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc, Guid? UpdatedByUserId)
{
    public static WorkspaceDocumentDto From(WorkspaceDocument document) => new(
        document.Id, document.WorkspaceId, document.Title, document.GoogleFileId, document.SourceType,
        document.MimeType, document.CreatedByUserId, document.CreatedAtUtc, document.UpdatedAtUtc, document.UpdatedByUserId);
}

public record RegisterWorkspaceDocumentRequestDto(string Title, string GoogleFileId,
    WorkspaceDocumentSource SourceType, string? MimeType = null);

public record RenameWorkspaceDocumentRequestDto(string Title);
