using Domain.Aggregates.Collaboration.Workspaces.Invitations.Enums;
using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Shared.Primitives;

namespace Domain.Aggregates.Collaboration.Workspaces.Invitations
{
    public class WorkspaceInvitation : Entity
    {
        private WorkspaceInvitation() { } // EF Core

        private WorkspaceInvitation(
            Guid id,
            Guid invitedUserId,
            Guid invitedBy,
            WorkspaceRole assignedRole,
            DateTime createdAtUtc,
            DateTime expiresAtUtc)
            : base(id)
        {
            InvitedUserId = invitedUserId;
            InvitedBy = invitedBy;
            AssignedRole = assignedRole;
            CreatedAtUtc = createdAtUtc;
            ExpiresAtUtc = expiresAtUtc;
            Status = WorkspaceInvitationStatus.Pending;
        }

        public Guid InvitedUserId { get; private set; }

        public Guid InvitedBy { get; private set; }

        public WorkspaceRole AssignedRole { get; private set; }

        public WorkspaceInvitationStatus Status { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime ExpiresAtUtc { get; private set; }

        public DateTime? AcceptedAtUtc { get; private set; }

        public DateTime? RevokedAtUtc { get; private set; }

        internal static WorkspaceInvitation Create(
            Guid invitedUserId,
            Guid invitedBy,
            WorkspaceRole assignedRole,
            DateTime nowUtc)
        {
            return new WorkspaceInvitation(
                Guid.NewGuid(),
                invitedUserId,
                invitedBy,
                assignedRole,
                nowUtc,
                nowUtc.AddDays(7));
        }

        internal bool IsPendingAt(DateTime nowUtc)
        {
            return Status == WorkspaceInvitationStatus.Pending
                && nowUtc < ExpiresAtUtc;
        }

        internal void Accept(DateTime nowUtc)
        {
            Status = WorkspaceInvitationStatus.Accepted;
            AcceptedAtUtc = nowUtc;
        }

        internal void Revoke(DateTime nowUtc)
        {
            Status = WorkspaceInvitationStatus.Revoked;
            RevokedAtUtc = nowUtc;
        }

        internal void MarkExpired()
        {
            Status = WorkspaceInvitationStatus.Expired;
        }
    }
}
