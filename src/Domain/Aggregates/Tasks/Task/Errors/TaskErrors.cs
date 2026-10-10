using SharedKernel.Shared;

namespace Domain.Aggregates.Tasks.Task.Errors
{
    public static class TaskErrors
    {
        public static Error InvalidStatus => Error.Failure("Invalid.Status", "Invalid task status.");
        public static Error InvalidAssignee => Error.Failure("Invalid.Assignee", "Cannot assign the same person twice on a task.");
        public static Error NullAssignee => Error.Failure("Null.Assignee", "Task has no assigned user.");
    }
}
