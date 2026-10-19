using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.User.Events;

public record PasswordChanged(Guid UserId, string Email) : IDomainEvent
{
    public string DomainEventName => DomainEventsNames.UserEventNames.PasswordChanged;
}