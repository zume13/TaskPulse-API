using Api.Constants;
using Api.Extensions;
using Application.Dto;
using Application.Features.Auth.Commands.ChangeRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = ApiConstants.AuthPolicies.AdminOnly)]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid userId, ChangeRoleRequestDto request, CancellationToken cancellationToken)
    {
        var command = new ChangeRoleCommand(userId, request.NewRole);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }
}