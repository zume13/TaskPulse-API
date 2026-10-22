using Domain.Shared.DomainEvent;

namespace Domain.Aggregates.User.Events;

public record VerificationEmailRequested(Guid UserId, string Email, string FirstName, string VerificationToken) : IDomainEvent
{
    public string DomainEventName => DomainEventsNames.UserEventNames.VerificationEmailRequested;
}