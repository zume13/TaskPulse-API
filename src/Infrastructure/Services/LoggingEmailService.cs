using Application.Abstractions.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LoggingEmailService(IConfiguration configuration, ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendVerificationEmailAsync(
        Guid userId,
        string email,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Email:VerificationBaseUrl"]
            ?? throw new InvalidOperationException("Configuration value 'Email:VerificationBaseUrl' is missing.");

        var verificationLink = $"{baseUrl}?userId={userId}&token={Uri.EscapeDataString(verificationToken)}";

        // temporary stand-in
        logger.LogInformation(
            "Verification email for {FirstName} <{Email}>: {VerificationLink}",
            firstName,
            email,
            verificationLink);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Email:PasswordResetBaseUrl"]
            ?? throw new InvalidOperationException("Configuration value 'Email:PasswordResetBaseUrl' is missing.");

        var resetLink = $"{baseUrl}?token={Uri.EscapeDataString(resetToken)}";

        // temporary stand-in
        logger.LogInformation(
            "Password reset email for {FirstName} <{Email}>: {ResetLink}",
            firstName,
            email,
            resetLink);

        return Task.CompletedTask;
    }
}