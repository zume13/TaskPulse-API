using Domain.Aggregates.LoginAudit.Enums;

namespace Application.Dto;

/// <summary>Query string of the admin list, e.g. ?outcome=InvalidCredentials&amp;from=2026-11-01T00:00:00Z.</summary>
public record LoginAuditListRequestDto(
    Guid? UserId = null,
    string? Email = null,
    LoginOutcome? Outcome = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20);