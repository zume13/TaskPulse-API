namespace Domain.Aggregates.Notifications.Enums
{
    public enum NotificationType
    {
        TaskAssigned = 1,
        TaskUnassigned = 2,
        TaskCompleted = 3,
        TaskCommentAdded = 4,
        WorkspaceInvitationReceived = 5,
        WorkspaceInvitationAccepted = 6,
        WorkspaceMemberRemoved = 7,
        WorkspaceRoleChanged = 8
    }
}
