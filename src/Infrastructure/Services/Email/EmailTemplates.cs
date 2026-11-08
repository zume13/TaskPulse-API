using System.Net;

namespace Infrastructure.Services.Mailing;

internal sealed record EmailContent(string Subject, string Html);

/// <summary>
/// HTML bodies of the four emails. Every value that came from a user (names,
/// workspace names) is HTML-encoded, so a name like "&lt;script&gt;" is shown
/// as text and never becomes markup in someone else's inbox.
/// </summary>
internal static class EmailTemplates
{
    public static EmailContent Verification(string firstName, string verificationLink) => new(
        "Verify your TaskPulse email",
        Layout(
            "Verify your email",
            $"""
            <p>Hi {Encode(firstName)},</p>
            <p>Welcome to TaskPulse! Please confirm your email address to activate your account.</p>
            {Button(verificationLink, "Verify email")}
            {LinkFallback(verificationLink)}
            <p style="color:#6b7280;font-size:13px;">If you did not create an account, you can ignore this email.</p>
            """));

    public static EmailContent PasswordReset(string firstName, string resetLink) => new(
        "Reset your TaskPulse password",
        Layout(
            "Reset your password",
            $"""
            <p>Hi {Encode(firstName)},</p>
            <p>We received a request to reset the password of your TaskPulse account. The link below is valid for a limited time.</p>
            {Button(resetLink, "Reset password")}
            {LinkFallback(resetLink)}
            <p style="color:#6b7280;font-size:13px;">If you did not ask for this, you can ignore this email. Your password will not change.</p>
            """));

    public static EmailContent PasswordChanged(string firstName) => new(
        "Your TaskPulse password was changed",
        Layout(
            "Your password was changed",
            $"""
            <p>Hi {Encode(firstName)},</p>
            <p>The password of your TaskPulse account was just changed.</p>
            <p style="color:#6b7280;font-size:13px;">If this was not you, reset your password right away and contact your administrator.</p>
            """));

    public static EmailContent WorkspaceInvitation(
        string firstName,
        string workspaceName,
        string invitedByName,
        string? appUrl) => new(
        $"{invitedByName} invited you to \"{workspaceName}\" on TaskPulse",
        Layout(
            "You have been invited",
            $"""
            <p>Hi {Encode(firstName)},</p>
            <p><strong>{Encode(invitedByName)}</strong> invited you to join the workspace <strong>{Encode(workspaceName)}</strong> on TaskPulse.</p>
            {(appUrl is null ? "<p>Log in to TaskPulse to accept the invitation.</p>" : Button(appUrl, "Open TaskPulse"))}
            """));

    private static string Layout(string title, string body) => $"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0;padding:24px;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
            <tr><td align="center">
              <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border-radius:8px;padding:32px;">
                <tr><td style="font-size:15px;line-height:1.5;">
                  <h1 style="margin:0 0 16px;font-size:20px;">{Encode(title)}</h1>
                  {body}
                  <p style="margin:32px 0 0;font-size:12px;color:#9ca3af;">TaskPulse</p>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    private static string Button(string url, string label) =>
        $"""<p style="margin:24px 0;"><a href="{Encode(url)}" style="display:inline-block;padding:12px 24px;background:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:bold;">{Encode(label)}</a></p>""";

    private static string LinkFallback(string url) =>
        $"""<p style="color:#6b7280;font-size:13px;">Or copy this link into your browser:<br>{Encode(url)}</p>""";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}