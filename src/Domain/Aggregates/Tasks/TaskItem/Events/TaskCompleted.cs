using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Tasks.Task.Events
{
    public record TaskCompleted(
        Guid TaskId,
        Guid WorkspaceId,
        string TaskTitle,
        Guid CompletedBy,
        Guid CreatedBy,
        Guid? AssignedTo) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.TaskEventNames.TaskCompleted;
    }
}
