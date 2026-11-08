using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Mailing;

/// <summary>
/// Email settings read from the "Email" section of the configuration.
/// The provider decides which IEmailService is used:
///   "Logging" (default): only writes the email to the log. For development.
///   "Brevo": sends real emails through Brevo's transactional API.
/// </summary>
public sealed record EmailOptions
{
    public const string LoggingProvider = "Logging";
    public const string BrevoProvider = "Brevo";

    public string Provider { get; init; } = LoggingProvider;

    /// <summary>The address emails are sent from. Must be a verified sender in Brevo.</summary>
    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = "TaskPulse";

    public string BrevoApiKey { get; init; } = string.Empty;

    /// <summary>Link target of the verification email. ?userId=...&amp;token=... is appended.</summary>
    public string VerificationBaseUrl { get; init; } = string.Empty;

    /// <summary>Link target of the password reset email. ?token=... is appended.</summary>
    public string PasswordResetBaseUrl { get; init; } = string.Empty;

    /// <summary>Optional. Where the "Open TaskPulse" button of the invitation email points.</summary>
    public string? AppUrl { get; init; }

    public bool UsesBrevo => Provider.Equals(BrevoProvider, StringComparison.OrdinalIgnoreCase);

    public static EmailOptions From(IConfiguration configuration)
    {
        var provider = configuration["Email:Provider"];

        return new EmailOptions
        {
            Provider = string.IsNullOrWhiteSpace(provider) ? LoggingProvider : provider.Trim(),
            FromEmail = configuration["Email:FromEmail"]?.Trim() ?? string.Empty,
            FromName = string.IsNullOrWhiteSpace(configuration["Email:FromName"])
                ? "TaskPulse"
                : configuration["Email:FromName"]!.Trim(),
            BrevoApiKey = configuration["Email:Brevo:ApiKey"]?.Trim() ?? string.Empty,
            VerificationBaseUrl = configuration["Email:VerificationBaseUrl"]?.Trim() ?? string.Empty,
            PasswordResetBaseUrl = configuration["Email:PasswordResetBaseUrl"]?.Trim() ?? string.Empty,
            AppUrl = string.IsNullOrWhiteSpace(configuration["Email:AppUrl"])
                ? null
                : configuration["Email:AppUrl"]!.Trim()
        };
    }

    /// <summary>
    /// Fails at startup, not at the first email, when the configuration is
    /// wrong. A typo in the provider name must not silently fall back to
    /// "log only" in production.
    /// </summary>
    public void Validate()
    {
        var isLogging = Provider.Equals(LoggingProvider, StringComparison.OrdinalIgnoreCase);

        if (!isLogging && !UsesBrevo)
            throw new InvalidOperationException(
                $"Unknown Email:Provider '{Provider}'. Use '{LoggingProvider}' or '{BrevoProvider}'.");

        if (!UsesBrevo)
            return;

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(BrevoApiKey))
            problems.Add("Email:Brevo:ApiKey");

        if (string.IsNullOrWhiteSpace(FromEmail))
            problems.Add("Email:FromEmail");

        if (!IsAbsoluteHttpUrl(VerificationBaseUrl))
            problems.Add("Email:VerificationBaseUrl");

        if (!IsAbsoluteHttpUrl(PasswordResetBaseUrl))
            problems.Add("Email:PasswordResetBaseUrl");

        if (AppUrl is not null && !IsAbsoluteHttpUrl(AppUrl))
            problems.Add("Email:AppUrl");

        if (problems.Count > 0)
            throw new InvalidOperationException(
                $"Email provider '{BrevoProvider}' is missing or has invalid configuration: {string.Join(", ", problems)}.");
    }

    private static bool IsAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}