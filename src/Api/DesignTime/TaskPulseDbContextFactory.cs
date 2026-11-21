using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.DesignTime;

/// <summary>
/// Offline EF scaffolding: does not execute Program, read secrets, seed, migrate or open a connection.
/// Applying migrations is a separate owner-approved operation requiring an explicit connection.
/// </summary>
public sealed class TaskPulseDbContextFactory : IDesignTimeDbContextFactory<TaskPulseDbContext>
{
    public TaskPulseDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<TaskPulseDbContext>().UseNpgsql().Options,
        TimeProvider.System);
}
