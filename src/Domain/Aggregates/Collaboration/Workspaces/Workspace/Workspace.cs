using Domain.Aggregates.Collaboration.Workspaces.Invitations;
using Domain.Aggregates.Collaboration.Workspaces.Invitations.Enums;
using Domain.Aggregates.Collaboration.Workspaces.Member;
using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.ValueObjects;
using Shared.ResultPattern;
using SharedKernel.Constants;
using SharedKernel.Primitives;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace
{
    public class Workspace : AggregateRoot
    {
        private readonly List<WorkspaceMember> _members = [];
        private readonly List<WorkspaceInvitation> _invitations = [];

        private Workspace() { } // EF Core

        private Workspace(
            Guid id,
            Name name,
            Guid createdBy,
            DateTime nowUtc)
            : base(id)
        {
            WorkspaceName = name;
            CreatedBy = createdBy;
            CreatedAtUtc = nowUtc;

            _members.Add(
                WorkspaceMember.Create(
                    createdBy,
                    WorkspaceRole.Owner,
                    nowUtc));
        }

        public Name WorkspaceName { get; private set; } = null!;

        public Guid CreatedBy { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public bool IsArchived { get; private set; }

        public DateTime? ArchivedAtUtc { get; private set; }

        public IReadOnlyCollection<WorkspaceMember> Members =>
            _members.AsReadOnly();

        public IReadOnlyCollection<WorkspaceInvitation> Invitations =>
            _invitations.AsReadOnly();

        public static ResultT<Workspace> Create(
            string name,
            Guid createdBy)
        {
            return Create(name, createdBy, SystemConstants.UtcNow);
        }

        public static ResultT<Workspace> Create(
            string name,
            Guid createdBy,
            DateTime nowUtc)
        {
            if (createdBy == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(createdBy));

            var workspaceName = Name.Create(name);

            if (workspaceName.IsFailure)
                return workspaceName.Error;

            var workspace = new Workspace(
                Guid.NewGuid(),
                workspaceName.value,
                createdBy,
                nowUtc);

            // The creator automatically becomes the Owner.
            workspace._members.Add(
                WorkspaceMember.Create(
                    createdBy,
                    WorkspaceRole.Owner,
                    nowUtc));

            return workspace;
        }

        public WorkspaceRole? GetMemberRole(Guid userId)
        {
            var member = _members.FirstOrDefault(
                x => x.UserId == userId);

            return member?.Role;
        }

        public bool IsMember(Guid userId)
        {
            return _members.Any(x => x.UserId == userId);
        }

        public Result Rename(
            Guid actorId,
            string name)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            if (!IsOwnerOrAdmin(actorId))
                return WorkspaceErrors.NotAuthorized;

            var workspaceName = Name.Create(name);

            if (workspaceName.IsFailure)
                return workspaceName.Error;

            WorkspaceName = workspaceName.value;

            return Result.Success();
        }

        public Result InviteMember(
            Guid invitedUserId,
            Guid invitedBy,
            WorkspaceRole role,
            DateTime nowUtc)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            if (!IsOwnerOrAdmin(invitedBy))
                return WorkspaceErrors.NotAuthorized;

            if (invitedUserId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(invitedUserId));

            if (invitedUserId == invitedBy)
                return WorkspaceErrors.InvalidInvitationTarget;

            if (!Enum.IsDefined(role))
                return WorkspaceErrors.InvalidWorkspaceRole;

            if (role == WorkspaceRole.Owner)
                return WorkspaceErrors.CannotInviteOwner;

            if (IsMember(invitedUserId))
                return WorkspaceErrors.AlreadyMember;

            var existingInvitation = _invitations.FirstOrDefault(
                x => x.InvitedUserId == invitedUserId
                    && x.Status == WorkspaceInvitationStatus.Pending
                    && nowUtc < x.ExpiresAtUtc);

            if (existingInvitation is not null)
                return WorkspaceErrors.InvitationAlreadyPending;

            // Expire any old pending invitations for this user.
            foreach (var invitation in _invitations.Where(
                x => x.InvitedUserId == invitedUserId
                    && x.Status == WorkspaceInvitationStatus.Pending
                    && nowUtc >= x.ExpiresAtUtc))
            {
                invitation.MarkExpired();
            }

            _invitations.Add(
                WorkspaceInvitation.Create(
                    invitedUserId,
                    invitedBy,
                    role,
                    nowUtc));

            // Raise MemberInvited domain event here if your
            // AggregateRoot supports the event type you use.

            return Result.Success();
        }

        public Result AcceptInvitation(
            Guid invitationId,
            Guid actorId,
            DateTime nowUtc)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            if (actorId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(actorId));

            var invitation = _invitations.FirstOrDefault(
                x => x.Id == invitationId);

            if (invitation is null)
                return WorkspaceErrors.InvitationNotFound;

            if (invitation.InvitedUserId != actorId)
                return WorkspaceErrors.NotAuthorized;

            if (invitation.Status != WorkspaceInvitationStatus.Pending)
                return WorkspaceErrors.InvitationNotPending;

            if (nowUtc >= invitation.ExpiresAtUtc)
            {
                invitation.MarkExpired();
                return WorkspaceErrors.InvitationExpired;
            }

            if (IsMember(actorId))
                return WorkspaceErrors.AlreadyMember;

            _members.Add(
                WorkspaceMember.Create(
                    actorId,
                    invitation.AssignedRole,
                    nowUtc));

            invitation.Accept(nowUtc);

            return Result.Success();
        }

        public Result RemoveMember(
            Guid actorId,
            Guid memberId,
            DateTime nowUtc)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            if (actorId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(actorId));

            if (memberId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(memberId));

            var actor = _members.FirstOrDefault(
                x => x.UserId == actorId);

            if (actor is null)
                return WorkspaceErrors.NotAuthorized;

            var member = _members.FirstOrDefault(
                x => x.UserId == memberId);

            if (member is null)
                return WorkspaceErrors.MemberNotFound;

            if (actor.Role != WorkspaceRole.Owner
                && actor.Role != WorkspaceRole.Admin)
            {
                return WorkspaceErrors.NotAuthorized;
            }

            if (member.Role == WorkspaceRole.Owner)
            {
                if (actor.Role != WorkspaceRole.Owner)
                    return WorkspaceErrors.NotAuthorized;

                if (_members.Count(x => x.Role == WorkspaceRole.Owner) <= 1)
                    return WorkspaceErrors.CannotRemoveLastOwner;
            }

            _members.Remove(member);

            // Revoke any outstanding invitations issued to the removed user.
            foreach (var invitation in _invitations.Where(
                x => x.InvitedUserId == memberId
                    && x.Status == WorkspaceInvitationStatus.Pending))
            {
                invitation.Revoke(nowUtc);
            }

            return Result.Success();
        }

        public Result ChangeMemberRole(
            Guid actorId,
            Guid memberId,
            WorkspaceRole newRole)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            if (actorId == Guid.Empty || memberId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(actorId));

            if (!Enum.IsDefined(newRole))
                return WorkspaceErrors.InvalidWorkspaceRole;

            // Only the owner can change roles in this implementation.
            var actor = _members.FirstOrDefault(
                x => x.UserId == actorId);

            if (actor?.Role != WorkspaceRole.Owner)
                return WorkspaceErrors.NotAuthorized;

            var member = _members.FirstOrDefault(
                x => x.UserId == memberId);

            if (member is null)
                return WorkspaceErrors.MemberNotFound;

            if (member.Role == WorkspaceRole.Owner
                && newRole != WorkspaceRole.Owner
                && _members.Count(x => x.Role == WorkspaceRole.Owner) <= 1)
            {
                return WorkspaceErrors.CannotRemoveLastOwner;
            }

            // Owner status must be transferred explicitly, not granted
            // by an ordinary member role-change operation.
            if (newRole == WorkspaceRole.Owner
                && member.Role != WorkspaceRole.Owner)
            {
                return WorkspaceErrors.UseOwnershipTransfer;
            }

            member.ChangeRole(newRole);

            return Result.Success();
        }

        public Result TransferOwnership(
            Guid currentOwnerId,
            Guid newOwnerId)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            if (currentOwnerId == Guid.Empty || newOwnerId == Guid.Empty)
                return WorkspaceErrors.InvalidWorkspaceId;

            var currentOwner = _members
                .FirstOrDefault(m => m.UserId == currentOwnerId);

            if (currentOwner is null || currentOwner.Role != WorkspaceRole.Owner)
                return WorkspaceErrors.NotAuthorized;

            var newOwner = _members
                .FirstOrDefault(m => m.UserId == newOwnerId);

            if (newOwner is null)
                return WorkspaceErrors.MemberNotFound;

            if (currentOwnerId == newOwnerId)
                return WorkspaceErrors.CannotTransferOwnershipToSelf;

            // Demote the current owner.
            currentOwner.ChangeRole(WorkspaceRole.Admin);

            // Promote the selected member to owner.
            newOwner.ChangeRole(WorkspaceRole.Owner);

            return Result.Success();
        }

        public Result Leave(
            Guid actorId,
            DateTime nowUtc)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            if (actorId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(actorId));

            var member = _members.FirstOrDefault(
                x => x.UserId == actorId);

            if (member is null)
                return WorkspaceErrors.MemberNotFound;

            if (member.Role == WorkspaceRole.Owner
                && _members.Count(x => x.Role == WorkspaceRole.Owner) <= 1)
            {
                return WorkspaceErrors.CannotLeaveAsLastOwner;
            }

            _members.Remove(member);

            foreach (var invitation in _invitations.Where(
                x => x.InvitedUserId == actorId
                    && x.Status == WorkspaceInvitationStatus.Pending))
            {
                invitation.Revoke(nowUtc);
            }

            return Result.Success();
        }

        public Result Archive(Guid actorId, DateTime nowUtc)
        {
            if (IsArchived)
                return WorkspaceErrors.WorkspaceDeletedOrArchived;

            var actor = _members.FirstOrDefault(
                x => x.UserId == actorId);

            if (actor?.Role != WorkspaceRole.Owner)
                return WorkspaceErrors.NotAuthorized;

            IsArchived = true;
            ArchivedAtUtc = nowUtc;

            foreach (var invitation in _invitations.Where(
                x => x.Status == WorkspaceInvitationStatus.Pending))
            {
                invitation.Revoke(nowUtc);
            }

            return Result.Success();
        }

        private bool IsOwnerOrAdmin(Guid userId)
        {
            var role = GetMemberRole(userId);

            return role is WorkspaceRole.Owner or WorkspaceRole.Admin;
        }
    }
}
