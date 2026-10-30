using Application.Abstractions.Auth;
using Application.Features.Notifications;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Events;
using Domain.Aggregates.User;
using Domain.Shared.DomainEvent;

namespace Application.Features.Workspace.Events;

/// <summary>
/// Emails the invited user. A separate handler from the notification one, so
/// the outbox job retries and tracks each of them on its own.
/// </summary>
public class WorkspaceInvitationEmailHandler(
    IEmailService emailService,
    IUserRepository userRepository)
    : IDomainEventHandler<MemberInvited>
{
    public async Task Handle(MemberInvited domainEvent, CancellationToken cancellationToken)
    {
        var invitedUser = await userRepository.GetByIdAsync(domainEvent.InvitedUserId, cancellationToken);

        if (invitedUser is null)
            return;

        var inviter = await userRepository.GetByIdAsync(domainEvent.InvitedBy, cancellationToken);

        await emailService.SendWorkspaceInvitationEmailAsync(
            invitedUser.Email.Value,
            invitedUser.FirstName.Value,
            domainEvent.WorkspaceName,
            UserNames.DisplayName(inviter),
            cancellationToken);
    }
}
