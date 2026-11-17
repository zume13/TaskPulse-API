using Api.Constants;
using Api.Extensions;
using Application.Dto;
using Application.Features.Users.Queries.GetAllUsersWithWorkspaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController(ISender sender) : ControllerBase
{
    /// <summary>Every user with all the workspaces they belong to, newest registrations first. Query: includeArchived (default true), page, pageSize.</summary>
    [Authorize(Policy = ApiConstants.AuthPolicies.AdminOnly)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] AdminListRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new GetAllUsersWithWorkspacesQuery(request.IncludeArchived, request.Page, request.PageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }
}
