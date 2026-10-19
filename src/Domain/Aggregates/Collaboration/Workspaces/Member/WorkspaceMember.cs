using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Shared.Primitives;

namespace Domain.Aggregates.Collaboration.Workspaces.Member
{
    public class WorkspaceMember : Entity
    {
        private WorkspaceMember() { } // EF Core

        private WorkspaceMember(
            Guid id,
            Guid userId,
            WorkspaceRole role,
            DateTime joinedAtUtc)
            : base(id)
        {
            UserId = userId;
            Role = role;
            JoinedAtUtc = joinedAtUtc;
        }

        public Guid UserId { get; private set; }

        public WorkspaceRole Role { get; private set; }

        public DateTime JoinedAtUtc { get; private set; }

        internal static WorkspaceMember Create(
            Guid userId,
            WorkspaceRole role,
            DateTime nowUtc)
        {
            return new WorkspaceMember(
                Guid.NewGuid(),
                userId,
                role,
                nowUtc);
        }

        internal void ChangeRole(WorkspaceRole role)
        {
            Role = role;
        }
    }
}
