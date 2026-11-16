using Api.Constants;
using Api.Extensions;
using Application.Dto;
using Application.Features.Auth.Queries.GetLoginAudits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/login-audits")]
public class LoginAuditsController(ISender sender) : ControllerBase
{
    /// <summary>All login attempts, newest first. Filters: userId, email, outcome, from, to.</summary>
    [Authorize(Policy = ApiConstants.AuthPolicies.AdminOnly)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] LoginAuditListRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new GetLoginAuditsQuery(
            request.UserId,
            request.Email,
            request.Outcome,
            request.From?.UtcDateTime,
            request.To?.UtcDateTime,
            request.Page,
            request.PageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }
}