using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.WorkspaceDocuments;

public static class WorkspaceDocumentErrors
{
    public static Error InvalidMetadata => Error.Validation("WorkspaceDocument.InvalidMetadata", "Document metadata is invalid.");
    public static Error NotFound => Error.NotFound("WorkspaceDocument.NotFound", "The document was not found.");
    public static Error Duplicate => Error.Conflict("WorkspaceDocument.Duplicate", "This Google file is already registered in the workspace.");
    public static Error NotAllowed => Error.Forbidden("WorkspaceDocument.NotAllowed", "Only the creator or a workspace owner/admin can change this document record.");
    public static Error Changed => Error.Conflict("WorkspaceDocument.Changed", "The document record changed during this operation. Reload and try again.");
}
