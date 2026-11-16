using Domain.Aggregates.LoginAudit.Enums;

namespace Application.Dto;

public record LoginAuditDto(
    Guid Id,
    Guid? UserId,
    string Email,
    LoginOutcome Outcome,
    string? IpAddress,
    string? UserAgent,
    DateTime OccurredAtUtc);
