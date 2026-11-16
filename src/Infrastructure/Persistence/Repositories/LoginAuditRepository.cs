using Domain.Aggregates.LoginAudit;
using Domain.Aggregates.LoginAudit.Enums;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class LoginAuditRepository
    : Repository<LoginAudit>, ILoginAuditRepository
{
    public LoginAuditRepository(TaskPulseDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<LoginAuditSearchResult> SearchAsync(
        Guid? userId,
        string? email,
        LoginOutcome? outcome,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<LoginAudit>().AsNoTracking();

        if (userId is { } userIdFilter)
            query = query.Where(audit => audit.UserId == userIdFilter);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            query = query.Where(audit => audit.Email == normalizedEmail);
        }

        if (outcome is { } outcomeFilter)
            query = query.Where(audit => audit.Outcome == outcomeFilter);

        if (fromUtc is { } fromValue)
            query = query.Where(audit => audit.OccurredAtUtc >= fromValue);

        if (toUtc is { } toValue)
            query = query.Where(audit => audit.OccurredAtUtc < toValue);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(audit => audit.OccurredAtUtc)
            .ThenBy(audit => audit.Id)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return new LoginAuditSearchResult(items, totalCount);
    }
}