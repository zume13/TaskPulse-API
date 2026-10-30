using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace.Events
{
    public record MemberInvited(
        Guid WorkspaceId,
        string WorkspaceName,
        Guid InvitationId,
        Guid InvitedUserId,
        Guid InvitedBy,
        WorkspaceRole Role,
        DateTime ExpiresAtUtc) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.WorkspaceEventNames.MemberInvited;
    }
}
