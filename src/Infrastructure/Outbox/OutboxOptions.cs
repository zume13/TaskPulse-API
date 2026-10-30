using Microsoft.Extensions.Configuration;

namespace Infrastructure.Outbox;

/// <summary>Read from the optional "Outbox" section of appsettings.json.</summary>
public sealed record OutboxOptions
{
    /// <summary>How often the Quartz job looks for new outbox messages.</summary>
    public int PollIntervalSeconds { get; init; } = 5;

    /// <summary>Messages picked up per run.</summary>
    public int BatchSize { get; init; } = 20;

    /// <summary>After this many failed attempts a message is dead-lettered.</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>How long a picked-up message is reserved for one job run.</summary>
    public int LeaseSeconds { get; init; } = 120;

    /// <summary>Processed messages older than this are deleted.</summary>
    public int RetentionDays { get; init; } = 7;

    public static OutboxOptions From(IConfiguration configuration)
    {
        return new OutboxOptions
        {
            PollIntervalSeconds = Read(configuration, "Outbox:PollIntervalSeconds", 5),
            BatchSize = Read(configuration, "Outbox:BatchSize", 20),
            MaxAttempts = Read(configuration, "Outbox:MaxAttempts", 5),
            LeaseSeconds = Read(configuration, "Outbox:LeaseSeconds", 120),
            RetentionDays = Read(configuration, "Outbox:RetentionDays", 7)
        };
    }

    private static int Read(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }
}
