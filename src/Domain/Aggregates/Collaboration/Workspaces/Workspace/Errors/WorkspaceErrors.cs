using Shared.ResultPattern;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors
{
    public static class WorkspaceErrors
    {
        public static Error InvalidWorkspaceId =>
            Error.Failure(
                "Workspace.InvalidId",
                "The workspace ID cannot be empty.");

        public static Error WorkspaceDeletedOrArchived =>
            Error.Failure(
                "Workspace.DeletedOrArchived",
                "Cannot perform this action on an archived workspace.");

        public static Error MemberNotFound =>
            Error.Failure(
                "Workspace.MemberNotFound",
                "The user is not a member of this workspace.");

        public static Error AlreadyMember =>
            Error.Failure(
                "Workspace.AlreadyMember",
                "The user is already a member of this workspace.");

        public static Error InvitationNotFound =>
            Error.Failure(
                "Workspace.InvitationNotFound",
                "The invitation was not found.");

        public static Error InvitationNotPending =>
            Error.Failure(
                "Workspace.InvitationNotPending",
                "The invitation is no longer pending.");

        public static Error CannotTransferOwnershipToSelf =>
            Error.Failure(
                "Workspace.CannotTransferOwnershipToSelf",
                "You cannot transfer ownership to yourself.");

        public static Error InvitationExpired =>
            Error.Failure(
                "Workspace.InvitationExpired",
                "The invitation has expired.");

        public static Error CannotInviteOwner =>
            Error.Failure(
                "Workspace.CannotInviteOwner",
                "Cannot assign the Owner role through an invitation.");

        public static Error CannotRemoveLastOwner =>
            Error.Failure(
                "Workspace.CannotRemoveLastOwner",
                "Cannot remove the last owner of the workspace.");

        public static Error CannotLeaveAsLastOwner =>
            Error.Failure(
                "Workspace.CannotLeaveAsLastOwner",
                "The last owner cannot leave the workspace.");

        public static Error InvalidWorkspaceRole =>
            Error.Failure(
                "Workspace.InvalidRole",
                "The specified workspace role is invalid.");

        public static Error NotAuthorized =>
            Error.Failure(
                "Workspace.NotAuthorized",
                "You are not authorized to perform this action.");

        public static Error InvalidInvitationTarget =>
            Error.Failure(
                "Workspace.InvalidInvitationTarget",
                "You cannot invite yourself to the workspace.");

        public static Error InvitationAlreadyPending =>
            Error.Failure(
                "Workspace.InvitationAlreadyPending",
                "A pending invitation already exists for this user.");

        public static Error WorkspaceNotFound =>
            Error.NotFound(
                "Workspace.NotFound",
                "The workspace was not found.");

        public static Error UseOwnershipTransfer =>
            Error.Failure(
                "Workspace.UseOwnershipTransfer",
                "Use the ownership transfer operation to assign the Owner role.");
    }
}
