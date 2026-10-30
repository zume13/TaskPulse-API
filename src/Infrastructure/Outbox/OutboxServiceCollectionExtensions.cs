using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Infrastructure.Outbox;

public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Quartz scheduler with the two outbox jobs. The scheduler
    /// starts and stops with the application.
    /// </summary>
    public static IServiceCollection AddOutboxProcessing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = OutboxOptions.From(configuration);

        services.AddSingleton(options);

        services.AddQuartz(quartz =>
        {
            var processJobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

            quartz.AddJob<ProcessOutboxMessagesJob>(job =>
                job.WithIdentity(processJobKey));

            quartz.AddTrigger(trigger => trigger
                .ForJob(processJobKey)
                .WithSimpleSchedule(schedule => schedule
                    .WithInterval(TimeSpan.FromSeconds(options.PollIntervalSeconds))
                    .RepeatForever()));

            var cleanJobKey = new JobKey(nameof(CleanOutboxMessagesJob));

            quartz.AddJob<CleanOutboxMessagesJob>(job =>
                job.WithIdentity(cleanJobKey));

            quartz.AddTrigger(trigger => trigger
                .ForJob(cleanJobKey)
                .WithSimpleSchedule(schedule => schedule
                    .WithInterval(TimeSpan.FromHours(1))
                    .RepeatForever()));
        });

        // Let a running job finish before the app shuts down.
        services.AddQuartzHostedService(hosted => hosted.WaitForJobsToComplete = true);

        return services;
    }
}
