using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Notifications;
using Domain.Aggregates.PasswordResetToken;
using Domain.Aggregates.RefreshToken;
using Domain.Aggregates.Tasks;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database;

public class TaskPulseDbContext(DbContextOptions<TaskPulseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskPulseDbContext).Assembly);
    }
}