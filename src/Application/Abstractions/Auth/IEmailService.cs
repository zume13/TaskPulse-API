namespace Application.Abstractions.Auth;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        Guid userId,
        string email,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetToken,
        CancellationToken cancellationToken = default);

    Task SendPasswordChangedEmailAsync(
        string email,
        string firstName,
        CancellationToken cancellationToken = default);

    Task SendWorkspaceInvitationEmailAsync(
        string email,
        string firstName,
        string workspaceName,
        string invitedByName,
        CancellationToken cancellationToken = default);
}
