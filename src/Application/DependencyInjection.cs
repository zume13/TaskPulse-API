using Application.Behaviours.Validations;
using Application.Features.Auth.Events;
using Domain.Aggregates.User.Events;
using Domain.Shared.DomainEvent;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddScoped<IDomainEventHandler<UserRegistered>, SendVerificationEmailHandler>();

        return services;
    }
}