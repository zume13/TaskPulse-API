using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace.Events
{
    public record MemberRoleChanged(
        Guid WorkspaceId,
        string WorkspaceName,
        Guid MemberId,
        WorkspaceRole OldRole,
        WorkspaceRole NewRole,
        Guid ChangedBy) : IDomainEvent
    {
        public string DomainEventName => DomainEventsNames.WorkspaceEventNames.MemberRoleChanged;
    }
}
