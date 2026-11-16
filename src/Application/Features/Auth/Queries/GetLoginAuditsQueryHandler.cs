using Application.Dto;
using Domain.Aggregates.LoginAudit;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Queries.GetLoginAudits;

public class GetLoginAuditsQueryHandler(ILoginAuditRepository loginAuditRepository)
    : IRequestHandler<GetLoginAuditsQuery, ResultT<PagedResultDto<LoginAuditDto>>>
{
    public async Task<ResultT<PagedResultDto<LoginAuditDto>>> Handle(
        GetLoginAuditsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await loginAuditRepository.SearchAsync(
            request.UserId,
            request.Email,
            request.Outcome,
            request.FromUtc,
            request.ToUtc,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        var items = result.Items
            .Select(audit => new LoginAuditDto(
                audit.Id,
                audit.UserId,
                audit.Email,
                audit.Outcome,
                audit.IpAddress,
                audit.UserAgent,
                audit.OccurredAtUtc))
            .ToList();

        return new PagedResultDto<LoginAuditDto>(items, request.Page, request.PageSize, result.TotalCount);
    }
}