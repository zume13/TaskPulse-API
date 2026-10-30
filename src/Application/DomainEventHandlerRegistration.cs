using Domain.Shared.DomainEvent;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DomainEventHandlerRegistration
{
    /// <summary>
    /// Registers every IDomainEventHandler&lt;TEvent&gt; in this assembly, so a
    /// new handler never needs a manual registration line. The Quartz outbox
    /// job resolves them by event type.
    /// </summary>
    public static IServiceCollection AddDomainEventHandlers(this IServiceCollection services)
    {
        var handlerInterface = typeof(IDomainEventHandler<>);

        var handlerTypes = typeof(DomainEventHandlerRegistration).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false });

        foreach (var handlerType in handlerTypes)
        {
            var implemented = handlerType.GetInterfaces().Where(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

            foreach (var serviceType in implemented)
                services.AddScoped(serviceType, handlerType);
        }

        return services;
    }
}
