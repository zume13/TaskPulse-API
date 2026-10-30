using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Tasks.Task.Events
{
    public record TaskAssigned(
        Guid TaskId,
        Guid WorkspaceId,
        string TaskTitle,
        Guid AssigneeId,
        Guid AssignedBy) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.TaskEventNames.TaskAssigned;
    }
}
