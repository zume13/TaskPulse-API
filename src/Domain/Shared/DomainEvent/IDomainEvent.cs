
namespace Domain.Shared.DomainEvent
{
    public interface IDomainEvent
    {
        string DomainEventName { get; }
    }
}
