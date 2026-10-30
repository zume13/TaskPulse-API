using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Tasks.Task.Events
{
    public record TaskUnassigned(
        Guid TaskId,
        Guid WorkspaceId,
        string TaskTitle,
        Guid PreviousAssigneeId,
        Guid UnassignedBy) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.TaskEventNames.TaskUnassigned;
    }
}
