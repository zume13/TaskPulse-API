using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace.Events
{
    public record MemberRemoved(
        Guid WorkspaceId,
        string WorkspaceName,
        Guid MemberId,
        Guid RemovedBy) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.WorkspaceEventNames.MemberRemoved;
    }
}
