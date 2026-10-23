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
}