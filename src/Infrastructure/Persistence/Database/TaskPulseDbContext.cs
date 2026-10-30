using System.Text.Json;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Notifications;
using Domain.Aggregates.PasswordResetToken;
using Domain.Aggregates.RefreshToken;
using Domain.Aggregates.Tasks;
using Domain.Aggregates.User;
using Domain.Shared.Primitives;
using Infrastructure.Outbox;
using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database;

public class TaskPulseDbContext(
    DbContextOptions<TaskPulseDbContext> options,
    TimeProvider timeProvider) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<OutboxMessageConsumer> OutboxMessageConsumers => Set<OutboxMessageConsumer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskPulseDbContext).Assembly);
    }

    // Every SaveChanges overload ends up in one of these two, so no save
    // path can skip the outbox.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddDomainEventsToOutbox();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AddDomainEventsToOutbox();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Turns the domain events raised by the tracked aggregates into outbox
    /// rows. They are saved by the same SaveChanges as the aggregate changes,
    /// in one transaction: an event is stored if and only if its change is.
    /// Nothing is dispatched here; the Quartz job runs the handlers later.
    /// </summary>
    private void AddDomainEventsToOutbox()
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
            return;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents.ToList())
            {
                OutboxMessages.Add(OutboxMessage.Create(
                    DomainEventTypes.NameOf(domainEvent),
                    JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), OutboxJson.Options),
                    nowUtc));

                aggregate.RemoveDomainEvent(domainEvent);
            }
        }
    }
}
