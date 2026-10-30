using Domain.Shared.DomainEvent;

namespace Infrastructure.Outbox;

/// <summary>
/// Maps the event name stored in the outbox back to its class. Only types
/// that implement IDomainEvent in the Domain assembly are known, so a stored
/// name can never make the job load an arbitrary type.
/// Renaming or moving an event class breaks messages that are still pending.
/// </summary>
internal static class DomainEventTypes
{
    private static readonly Dictionary<string, Type> Known = typeof(IDomainEvent).Assembly
        .GetTypes()
        .Where(type => type is { IsAbstract: false, IsInterface: false }
            && typeof(IDomainEvent).IsAssignableFrom(type))
        .ToDictionary(type => type.FullName!);

    public static string NameOf(IDomainEvent domainEvent) => domainEvent.GetType().FullName!;

    public static Type? Resolve(string name) => Known.GetValueOrDefault(name);
}
