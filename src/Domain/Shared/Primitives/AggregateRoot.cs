using LMS.SharedKernel.Primitives;
using SharedKernel.DomainEvent;

namespace SharedKernel.Primitives
{
    public abstract class AggregateRoot : Entity
    {
        protected AggregateRoot(Guid id) : base(id) { }
        protected AggregateRoot() { }

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        public void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }
    }
}
