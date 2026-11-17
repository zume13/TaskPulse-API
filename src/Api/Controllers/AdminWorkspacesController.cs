using Api.Constants;
using Api.Extensions;
using Application.Dto;
using Application.Features.Workspace.Queries.GetAllWorkspaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/workspaces")]
public class AdminWorkspacesController(ISender sender) : ControllerBase
{
    /// <summary>Every workspace with all of its members, newest first. Query: includeArchived (default true), page, pageSize.</summary>
    [Authorize(Policy = ApiConstants.AuthPolicies.AdminOnly)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] AdminListRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new GetAllWorkspacesQuery(request.IncludeArchived, request.Page, request.PageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }
}
