using Application.Dto;
using Domain.Aggregates.LoginAudit.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Queries.GetLoginAudits;

public record GetLoginAuditsQuery(
    Guid? UserId,
    string? Email,
    LoginOutcome? Outcome,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize) : IRequest<ResultT<PagedResultDto<LoginAuditDto>>>;