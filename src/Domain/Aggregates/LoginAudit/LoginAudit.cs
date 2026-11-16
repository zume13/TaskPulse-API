using Domain.Aggregates.LoginAudit.Enums;
using Domain.Shared.Primitives;

namespace Domain.Aggregates.LoginAudit;

/// <summary>
/// One login attempt. It is a record of something that happened, so it is
/// created once and never changed.
/// </summary>
public class LoginAudit : AggregateRoot
{
    public const int MaxEmailLength = 256;
    public const int MaxIpAddressLength = 45;   // longest textual IPv6 address
    public const int MaxUserAgentLength = 512;

    private LoginAudit() { }

    private LoginAudit(
        Guid id,
        Guid? userId,
        string email,
        LoginOutcome outcome,
        string? ipAddress,
        string? userAgent,
        DateTime occurredAtUtc) : base(id)
    {
        UserId = userId;
        Email = email;
        Outcome = outcome;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Null when the email does not belong to any account.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>The email that was typed, lower-cased.</summary>
    public string Email { get; private set; } = null!;

    public LoginOutcome Outcome { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public static LoginAudit Create(
        Guid? userId,
        string email,
        LoginOutcome outcome,
        string? ipAddress,
        string? userAgent,
        DateTime occurredAtUtc)
    {
        // The values come from the request, so they are cut to the column sizes
        // instead of letting a very long header break the save.
        return new LoginAudit(
            Guid.NewGuid(),
            userId,
            Truncate(email?.ToLowerInvariant(), MaxEmailLength) ?? string.Empty,
            outcome,
            Truncate(ipAddress, MaxIpAddressLength),
            Truncate(userAgent, MaxUserAgentLength),
            occurredAtUtc);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}