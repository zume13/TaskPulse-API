using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace.Events
{
    public record MemberInvited() : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.WorkspaceEventNames.MemberInvited;   
    }
}
