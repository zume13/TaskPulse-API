using Application.Features.Notifications;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Events;
using Domain.Aggregates.Notifications;
using Domain.Aggregates.Notifications.Enums;
using Domain.Aggregates.User;
using Domain.Shared.DomainEvent;

namespace Application.Features.Workspace.Events;

/// <summary>
/// Creates in-app notifications for workspace events. Runs in the background
/// (Quartz outbox job), after the change that raised the event was committed.
/// </summary>
public class WorkspaceNotificationHandlers(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider)
    : IDomainEventHandler<MemberInvited>,
      IDomainEventHandler<InvitationAccepted>,
      IDomainEventHandler<MemberRemoved>,
      IDomainEventHandler<MemberRoleChanged>
{
    public async Task Handle(MemberInvited domainEvent, CancellationToken cancellationToken)
    {
        var inviter = await userRepository.GetByIdAsync(domainEvent.InvitedBy, cancellationToken);

        await NotificationHelper.AddAsync(
            notificationRepository,
            timeProvider,
            domainEvent.InvitedUserId,
            NotificationType.WorkspaceInvitationReceived,
            "Workspace invitation",
            $"{UserNames.DisplayName(inviter)} invited you to join \"{domainEvent.WorkspaceName}\" as {domainEvent.Role}.",
            domainEvent.WorkspaceId,
            NotificationTarget.Invitation,
            domainEvent.InvitationId,
            cancellationToken);
    }

    public async Task Handle(InvitationAccepted domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.MemberId == domainEvent.InvitedBy)
            return;

        var member = await userRepository.GetByIdAsync(domainEvent.MemberId, cancellationToken);

        await NotificationHelper.AddAsync(
            notificationRepository,
            timeProvider,
            domainEvent.InvitedBy,
            NotificationType.WorkspaceInvitationAccepted,
            "Invitation accepted",
            $"{UserNames.DisplayName(member)} joined \"{domainEvent.WorkspaceName}\".",
            domainEvent.WorkspaceId,
            NotificationTarget.Workspace,
            domainEvent.WorkspaceId,
            cancellationToken);
    }

    public async Task Handle(MemberRemoved domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.MemberId == domainEvent.RemovedBy)
            return;

        // No workspace id or target: the user can no longer open this workspace.
        await NotificationHelper.AddAsync(
            notificationRepository,
            timeProvider,
            domainEvent.MemberId,
            NotificationType.WorkspaceMemberRemoved,
            "Removed from a workspace",
            $"You were removed from \"{domainEvent.WorkspaceName}\".",
            null,
            NotificationTarget.None,
            null,
            cancellationToken);
    }

    public async Task Handle(MemberRoleChanged domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.MemberId == domainEvent.ChangedBy)
            return;

        await NotificationHelper.AddAsync(
            notificationRepository,
            timeProvider,
            domainEvent.MemberId,
            NotificationType.WorkspaceRoleChanged,
            "Your role changed",
            $"Your role in \"{domainEvent.WorkspaceName}\" changed from {domainEvent.OldRole} to {domainEvent.NewRole}.",
            domainEvent.WorkspaceId,
            NotificationTarget.Workspace,
            domainEvent.WorkspaceId,
            cancellationToken);
    }
}
