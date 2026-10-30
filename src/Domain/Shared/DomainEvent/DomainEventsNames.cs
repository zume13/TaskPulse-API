namespace Domain.Shared.DomainEvent;

public static class DomainEventsNames
{
    public static class UserEventNames
    {
        public const string UserRegistered = "UserRegistered";
        public const string PasswordChanged = "PasswordChanged";
        public const string VerificationEmailRequested = "VerificationEmailRequested";
    }

    public static class WorkspaceEventNames
    {
        public const string MemberInvited = "MemberInvited";
        public const string InvitationAccepted = "InvitationAccepted";
        public const string MemberRemoved = "MemberRemoved";
        public const string MemberRoleChanged = "MemberRoleChanged";
    }

    public static class TaskEventNames
    {
        public const string TaskAssigned = "TaskAssigned";
        public const string TaskUnassigned = "TaskUnassigned";
        public const string TaskCompleted = "TaskCompleted";
        public const string CommentAdded = "CommentAdded";
    }
}
