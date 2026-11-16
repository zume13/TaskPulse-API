using Domain.Aggregates.LoginAudit.Enums;
using Domain.Shared.Base;

namespace Domain.Aggregates.LoginAudit;

public sealed record LoginAuditSearchResult(IReadOnlyList<LoginAudit> Items, int TotalCount);

public interface ILoginAuditRepository : IRepository<LoginAudit>
{
    /// <summary>Read-only, newest first. Every filter is optional. ToUtc is exclusive.</summary>
    Task<LoginAuditSearchResult> SearchAsync(
        Guid? userId,
        string? email,
        LoginOutcome? outcome,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}