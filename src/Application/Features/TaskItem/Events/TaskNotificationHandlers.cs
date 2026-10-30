using Application.Features.Notifications;
using Domain.Aggregates.Notifications;
using Domain.Aggregates.Notifications.Enums;
using Domain.Aggregates.Tasks.Task.Events;
using Domain.Aggregates.Tasks.TaskComment.Events;
using Domain.Aggregates.User;
using Domain.Shared.DomainEvent;

namespace Application.Features.Tasks.Events;

/// <summary>
/// Creates in-app notifications for task events. Runs in the background
/// (Quartz outbox job), after the change that raised the event was committed.
/// </summary>
public class TaskNotificationHandlers(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider)
    : IDomainEventHandler<TaskAssigned>,
      IDomainEventHandler<TaskUnassigned>,
      IDomainEventHandler<TaskCompleted>,
      IDomainEventHandler<CommentAdded>
{
    public async Task Handle(TaskAssigned domainEvent, CancellationToken cancellationToken)
    {
        // Assigning a task to yourself does not need a notification.
        if (domainEvent.AssigneeId == domainEvent.AssignedBy)
            return;

        var assigner = await userRepository.GetByIdAsync(domainEvent.AssignedBy, cancellationToken);

        await NotificationHelper.AddAsync(
            notificationRepository,
            timeProvider,
            domainEvent.AssigneeId,
            NotificationType.TaskAssigned,
            "New task assigned",
            $"{UserNames.DisplayName(assigner)} assigned you to \"{domainEvent.TaskTitle}\".",
            domainEvent.WorkspaceId,
            NotificationTarget.Task,
            domainEvent.TaskId,
            cancellationToken);
    }

    public async Task Handle(TaskUnassigned domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.PreviousAssigneeId == domainEvent.UnassignedBy)
            return;

        await NotificationHelper.AddAsync(
            notificationRepository,
            timeProvider,
            domainEvent.PreviousAssigneeId,
            NotificationType.TaskUnassigned,
            "Removed from a task",
            $"You were unassigned from \"{domainEvent.TaskTitle}\".",
            domainEvent.WorkspaceId,
            NotificationTarget.Task,
            domainEvent.TaskId,
            cancellationToken);
    }

    public async Task Handle(TaskCompleted domainEvent, CancellationToken cancellationToken)
    {
        var recipients = Recipients(
            domainEvent.CompletedBy,
            domainEvent.CreatedBy,
            domainEvent.AssignedTo);

        if (recipients.Count == 0)
            return;

        var completedBy = await userRepository.GetByIdAsync(domainEvent.CompletedBy, cancellationToken);

        foreach (var recipientId in recipients)
        {
            await NotificationHelper.AddAsync(
                notificationRepository,
                timeProvider,
                recipientId,
                NotificationType.TaskCompleted,
                "Task completed",
                $"{UserNames.DisplayName(completedBy)} marked \"{domainEvent.TaskTitle}\" as completed.",
                domainEvent.WorkspaceId,
                NotificationTarget.Task,
                domainEvent.TaskId,
                cancellationToken);
        }
    }

    public async Task Handle(CommentAdded domainEvent, CancellationToken cancellationToken)
    {
        var recipients = Recipients(
            domainEvent.AuthorId,
            domainEvent.TaskCreatedBy,
            domainEvent.TaskAssignedTo);

        if (recipients.Count == 0)
            return;

        var author = await userRepository.GetByIdAsync(domainEvent.AuthorId, cancellationToken);

        foreach (var recipientId in recipients)
        {
            await NotificationHelper.AddAsync(
                notificationRepository,
                timeProvider,
                recipientId,
                NotificationType.TaskCommentAdded,
                "New comment",
                $"{UserNames.DisplayName(author)} commented on \"{domainEvent.TaskTitle}\".",
                domainEvent.WorkspaceId,
                NotificationTarget.Task,
                domainEvent.TaskId,
                cancellationToken);
        }
    }

    /// <summary>The task's creator and assignee, minus whoever did the action.</summary>
    private static List<Guid> Recipients(Guid actorId, Guid createdBy, Guid? assignedTo)
    {
        var recipients = new List<Guid>(2);

        if (createdBy != actorId)
            recipients.Add(createdBy);

        if (assignedTo is { } assignee && assignee != actorId && assignee != createdBy)
            recipients.Add(assignee);

        return recipients;
    }
}
