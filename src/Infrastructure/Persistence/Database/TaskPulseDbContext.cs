using Domain.Aggregates.RefreshToken;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database;

public class TaskPulseDbContext(DbContextOptions<TaskPulseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskPulseDbContext).Assembly);
    }
}