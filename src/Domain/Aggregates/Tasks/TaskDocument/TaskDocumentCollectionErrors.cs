using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.TaskDocument;

public static class TaskDocumentCollectionErrors
{
    public static Error NotFound => Error.NotFound("TaskDocument.NotFound", "The task document was not found.");
    public static Error Duplicate => Error.Conflict("TaskDocument.Duplicate", "This URL is already attached to the task.");
    public static Error Changed => Error.Conflict("TaskDocument.Changed", "The document changed during this operation. Reload before trying again.");
}
