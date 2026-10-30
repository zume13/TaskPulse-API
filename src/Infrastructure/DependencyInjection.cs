using Application.Abstractions.Auth;
using Domain.Aggregates.Notifications;
using Domain.Aggregates.RefreshToken;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Infrastructure.Outbox;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Repositories;
using Infrastructure.Seeding;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Domain.Aggregates.PasswordResetToken;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<TaskPulseDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISecureTokenService, SecureTokenService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailService, LoggingEmailService>();

        services.AddScoped<AdminSeeder>();

        services.AddSingleton<TimeProvider>(TimeProvider.System);

        // Quartz jobs that run the domain event handlers from the outbox table.
        services.AddOutboxProcessing(configuration);

        return services;
    }
}