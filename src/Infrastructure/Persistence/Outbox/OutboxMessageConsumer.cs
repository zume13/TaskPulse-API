namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// Records that one handler has finished for one outbox message. If a later
/// handler of the same event fails and the message is retried, handlers that
/// already succeeded are skipped, so an email is not sent twice and a
/// notification is not created twice.
/// </summary>
public sealed class OutboxMessageConsumer
{
    private OutboxMessageConsumer() { } // EF Core

    public Guid OutboxMessageId { get; private set; }

    /// <summary>Full name of the handler class.</summary>
    public string Handler { get; private set; } = null!;

    public DateTime ConsumedAtUtc { get; private set; }

    public static OutboxMessageConsumer Create(Guid outboxMessageId, string handler, DateTime nowUtc)
    {
        return new OutboxMessageConsumer
        {
            OutboxMessageId = outboxMessageId,
            Handler = handler,
            ConsumedAtUtc = nowUtc
        };
    }
}
