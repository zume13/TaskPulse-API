using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.Auth;
using Infrastructure.Services.Mailing;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Email;

/// <summary>
/// Sends the application's emails through Brevo's transactional email API
/// (POST /v3/smtp/email). The api-key header and the base address are set
/// where the HttpClient is registered (EmailServiceCollectionExtensions).
/// Any failure throws EmailDeliveryException; retries belong to the outbox job.
/// </summary>
internal sealed class BrevoEmailService(
    HttpClient httpClient,
    EmailOptions options,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    private const string SendEmailPath = "v3/smtp/email";
    private const int MaxErrorBodyLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task SendVerificationEmailAsync(
        Guid userId,
        string email,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var link = WithQuery(
            options.VerificationBaseUrl,
            ("userId", userId.ToString()),
            ("token", verificationToken));

        return SendAsync(email, firstName, EmailTemplates.Verification(firstName, link), cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var link = WithQuery(options.PasswordResetBaseUrl, ("token", resetToken));

        return SendAsync(email, firstName, EmailTemplates.PasswordReset(firstName, link), cancellationToken);
    }

    public Task SendPasswordChangedEmailAsync(
        string email,
        string firstName,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(email, firstName, EmailTemplates.PasswordChanged(firstName), cancellationToken);
    }

    public Task SendWorkspaceInvitationEmailAsync(
        string email,
        string firstName,
        string workspaceName,
        string invitedByName,
        CancellationToken cancellationToken = default)
    {
        var content = EmailTemplates.WorkspaceInvitation(firstName, workspaceName, invitedByName, options.AppUrl);

        return SendAsync(email, firstName, content, cancellationToken);
    }

    private async Task SendAsync(
        string toEmail,
        string toName,
        EmailContent content,
        CancellationToken cancellationToken)
    {
        var request = new BrevoSendRequest(
            new BrevoAddress(options.FromEmail, options.FromName),
            [new BrevoAddress(toEmail, toName)],
            content.Subject,
            content.Html);

        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(SendEmailPath, request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new EmailDeliveryException("Could not reach Brevo.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout, not a shutdown. The outbox job retries it.
            throw new EmailDeliveryException("The request to Brevo timed out.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                throw new EmailDeliveryException(
                    $"Brevo rejected the email with status {(int)response.StatusCode} ({response.StatusCode}). "
                    + $"{Hint(response.StatusCode)} Response: {Truncate(body)}");
            }

            // The email is already accepted at this point. Whatever happens
            // while reading the reply must not throw, or the outbox would
            // retry and send the email a second time.
            var messageId = await TryReadMessageIdAsync(response, cancellationToken);

            logger.LogInformation(
                "Email \"{Subject}\" accepted by Brevo. MessageId: {MessageId}",
                content.Subject,
                messageId ?? "unknown");
        }
    }

    private static async Task<string?> TryReadMessageIdAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await response.Content.ReadFromJsonAsync<BrevoSendResponse>(JsonOptions, cancellationToken);

            return result?.MessageId;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or HttpRequestException)
        {
            return null;
        }
    }

    private static string Hint(System.Net.HttpStatusCode status) => status switch
    {
        System.Net.HttpStatusCode.Unauthorized => "Check Email:Brevo:ApiKey.",
        System.Net.HttpStatusCode.Forbidden => "Check the API key and that the sender is allowed in your Brevo account.",
        System.Net.HttpStatusCode.BadRequest => "Check that Email:FromEmail is a verified sender in Brevo.",
        System.Net.HttpStatusCode.TooManyRequests => "Brevo rate or daily limit reached; it will be retried.",
        _ => string.Empty
    };

    private static string Truncate(string value) =>
        value.Length > MaxErrorBodyLength ? value[..MaxErrorBodyLength] : value;

    private static string WithQuery(string baseUrl, params (string Key, string Value)[] parameters)
    {
        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var separator = baseUrl.Contains('?') ? "&" : "?";

        return $"{baseUrl}{separator}{query}";
    }

    private sealed record BrevoAddress(string Email, string Name);

    private sealed record BrevoSendRequest(
        BrevoAddress Sender,
        BrevoAddress[] To,
        string Subject,
        string HtmlContent);

    private sealed record BrevoSendResponse(string? MessageId);
}