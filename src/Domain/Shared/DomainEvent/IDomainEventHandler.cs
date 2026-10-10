
namespace SharedKernel.DomainEvent
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
    }
}
