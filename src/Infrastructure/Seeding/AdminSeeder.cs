using Application.Abstractions.Auth;
using Domain.Aggregates.User;
using Domain.Aggregates.User.Enum;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Seeding;

public class AdminSeeder(
    TaskPulseDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISecureTokenService secureTokenService,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin seeding skipped because AdminSeed:Email or AdminSeed:Password is not set.");
            return;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var accountExists = await dbContext.Users.AnyAsync(user => user.Email.Value == normalizedEmail, cancellationToken);

        if (accountExists)
        {
            logger.LogInformation("Admin seeding skipped because an account with the configured email already exists.");
            return;
        }

        var verificationToken = secureTokenService.GenerateToken();

        var userResult = User.Create(
            normalizedEmail,
            passwordHasher.Hash(password),
            configuration["AdminSeed:FirstName"] ?? "System",
            configuration["AdminSeed:LastName"] ?? "Admin",
            Role.Admin,
            verificationToken,
            timeProvider.GetUtcNow().UtcDateTime);

        if (userResult.IsFailure)
        {
            logger.LogError("Admin seeding failed: {Code} {Message}", userResult.Error.code, userResult.Error.message);
            return;
        }

        var admin = userResult.value;

        var verifyResult = admin.VerifyEmail(verificationToken);

        if (verifyResult.IsFailure)
        {
            logger.LogError("Admin seeding failed: {Code} {Message}", verifyResult.Error.code, verifyResult.Error.message);
            return;
        }

        dbContext.Users.Add(admin);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded the initial admin account for {Email}.", normalizedEmail);
    }
}