namespace Domain.Aggregates.Notifications.Enums
{
    /// <summary>
    /// What a notification points at, so the client knows where to navigate
    /// when the user clicks it.
    /// </summary>
    public enum NotificationTarget
    {
        None = 0,
        Task = 1,
        Workspace = 2,
        Invitation = 3
    }
}
