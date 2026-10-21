using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.Task.Errors
{
    public static class TaskErrors
    {
        public static Error InvalidStatus => Error.Failure("Invalid.Status", "Invalid task status.");
        public static Error InvalidAssignee => Error.Failure("Invalid.Assignee", "Cannot assign the same person twice on a task.");
        public static Error NullAssignee => Error.Failure("Null.Assignee", "Task has no assigned user.");
        public static Error InvalidDueDate => Error.Failure("Invalid.DueDate", "Due date cannot be in the past.");
        public static Error InvalidPriority => Error.Failure("Invalid.Priority", "Invalid task priority.");
        public static Error TaskDeleted => Error.Failure("Task.Deleted", "Cannot perform this action on a deleted task.");
    }

    public static class TaskCommentErrors
    {
        public static Error InvalidComment => Error.Failure("Invalid.Comment", "Cannot add the same comment twice on a task.");
        public static Error CommentDeleted => Error.Failure("Comment.Deleted", "Cannot perform this action on a deleted comment.");
        public static Error CommentNotFound => Error.Failure("Comment.NotFound", "The comment was not found.");
        public static Error CommentNotOwnedByUser => Error.Failure("Comment.NotOwnedByUser", "The comment is not owned by the user.");
    }

    public static class TaskDocumentErrors
    {
        public static Error InvalidUrl => Error.Failure("Invalid.Document", "Cannot add a document with an invalid URL.");
        public static Error InvalidUriScheme => Error.Failure("Invalid.Document", "Only HTTPS URLs are allowed.");
        public static Error InvalidHost => Error.Failure("Invalid.Document", "Only Google Docs URLs are allowed.");
        public static Error InvalidAbsolutePath => Error.Failure("Invalid.Document", "The URL must point to a Google Docs document.");
        public static Error LabelTooLong => Error.Failure("Invalid.Document", $"The document label has exceeded the maximum length of characters.");
    }
}
