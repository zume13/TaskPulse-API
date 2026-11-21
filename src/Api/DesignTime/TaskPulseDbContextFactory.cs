using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.DesignTime;

/// <summary>Offline scaffolding only; no configuration, connection, startup, seeding or migrations executed.</summary>
public sealed class TaskPulseDbContextFactory : IDesignTimeDbContextFactory<TaskPulseDbContext>
{
    public TaskPulseDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<TaskPulseDbContext>().UseNpgsql().Options, TimeProvider.System);
}
