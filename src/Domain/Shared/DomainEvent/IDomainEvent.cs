
namespace SharedKernel.DomainEvent
{
    public interface IDomainEvent
    {
        string DomainEventName { get; }
    }
}
