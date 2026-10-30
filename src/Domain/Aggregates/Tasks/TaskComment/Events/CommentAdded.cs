using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Tasks.TaskComment.Events
{
    public record CommentAdded(
        Guid TaskId,
        Guid WorkspaceId,
        string TaskTitle,
        Guid CommentId,
        Guid AuthorId,
        Guid TaskCreatedBy,
        Guid? TaskAssignedTo) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.TaskEventNames.CommentAdded;
    }
}
