using Application.Behaviours.Validations;
using Application.Features.Auth.Events;
using Domain.Aggregates.User.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvent;

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

        // to be added
        //services.AddScoped<IDomainEventHandler<UserRegistered>, SendVerificationEmailHandler>();

        return services;
    }
}