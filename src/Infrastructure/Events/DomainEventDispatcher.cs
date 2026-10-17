using Application.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvent;

namespace Infrastructure.Events;

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            using var scope = serviceProvider.CreateScope();

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod("Handle")!;

            foreach (var handler in scope.ServiceProvider.GetServices(handlerType))
            {
                var task = (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;

                await task;
            }
        }
    }
}