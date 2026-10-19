using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.User.Events;

public record UserRegistered(Guid UserId, string Email, string FirstName, string VerificationToken) : IDomainEvent
{
    public string DomainEventName => DomainEventsNames.UserEventNames.UserRegistered;
}