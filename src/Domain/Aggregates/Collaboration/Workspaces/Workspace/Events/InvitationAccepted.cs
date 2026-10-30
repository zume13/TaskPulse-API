using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace.Events
{
    public record InvitationAccepted(
        Guid WorkspaceId,
        string WorkspaceName,
        Guid InvitationId,
        Guid MemberId,
        Guid InvitedBy) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.WorkspaceEventNames.InvitationAccepted;
    }
}
