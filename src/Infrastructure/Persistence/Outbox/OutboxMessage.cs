namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// One row per domain event raised by an aggregate. It is written in the same
/// transaction as the aggregate change (see TaskPulseDbContext.SaveChanges),
/// and processed later by the Quartz job ProcessOutboxMessagesJob, which runs
/// the event's handlers. Delivery is at-least-once.
/// </summary>
public sealed class OutboxMessage
{
    private const int MaxErrorLength = 2000;
    private const int MaxBackoffSeconds = 600;

    private OutboxMessage() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Full name of the domain event class, e.g. Domain.Aggregates.Tasks.Task.Events.TaskAssigned.</summary>
    public string Type { get; private set; } = null!;

    /// <summary>JSON of the domain event. Wiped once the message is processed.</summary>
    public string Payload { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime NextAttemptAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>
    /// A job that picked this message up holds it until this time, so a
    /// second app instance does not process it at the same moment. If the
    /// process dies, the lease simply expires and the message is retried.
    /// </summary>
    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    /// <summary>Set when attempts ran out (dead-lettered). Kept for inspection.</summary>
    public DateTime? FailedAtUtc { get; private set; }

    public static OutboxMessage Create(string type, string payload, DateTime nowUtc)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            OccurredAtUtc = nowUtc,
            NextAttemptAtUtc = nowUtc
        };
    }

    public void Lease(DateTime untilUtc)
    {
        LockedUntilUtc = untilUtc;
    }

    public void MarkProcessed(DateTime nowUtc)
    {
        ProcessedAtUtc = nowUtc;
        LockedUntilUtc = null;
        LastError = null;

        // Events such as UserRegistered carry one-time tokens. Once the
        // handlers have run, the payload is no longer needed.
        Payload = "{}";
    }

    public void MarkFailed(string error, DateTime nowUtc, int maxAttempts)
    {
        Attempts++;
        LockedUntilUtc = null;

        LastError = error.Length > MaxErrorLength
            ? error[..MaxErrorLength]
            : error;

        if (Attempts >= maxAttempts)
        {
            FailedAtUtc = nowUtc;
            return;
        }

        // 10s, 20s, 40s, 80s ... capped at 10 minutes.
        var delaySeconds = Math.Min(5 * Math.Pow(2, Attempts), MaxBackoffSeconds);
        NextAttemptAtUtc = nowUtc.AddSeconds(delaySeconds);
    }
}
