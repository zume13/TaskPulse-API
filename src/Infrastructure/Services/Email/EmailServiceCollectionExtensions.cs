using Application.Abstractions.Auth;
using Infrastructure.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace Infrastructure.Services.Mailing;

public static class EmailServiceCollectionExtensions
{
    private const string BrevoBaseAddress = "https://api.brevo.com/";

    /// <summary>
    /// Registers IEmailService. Email:Provider = "Brevo" sends real emails,
    /// anything else (default "Logging") only logs them.
    /// </summary>
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = EmailOptions.From(configuration);

        options.Validate();

        services.AddSingleton(options);

        if (!options.UsesBrevo)
        {
            services.AddScoped<IEmailService, LoggingEmailService>();

            return services;
        }

        services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
        {
            client.BaseAddress = new Uri(BrevoBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("api-key", options.BrevoApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}