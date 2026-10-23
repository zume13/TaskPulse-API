using Api.Constants;
using Api.Extensions;
using Application.Dto;
using Application.Features.Auth.Commands.ChangeRole;
using Application.Features.Users.Commands.ChangeEmail;
using Application.Features.Users.Commands.UpdateProfile;
using Application.Features.Users.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpGet("get-user")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new GetCurrentUserQuery(userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }

    [Authorize]
    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new UpdateProfileCommand(userId.Value, request.FirstName, request.LastName);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }

    [Authorize]
    [HttpPut("change-email")]
    public async Task<IActionResult> ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new ChangeEmailCommand(userId.Value, request.NewEmail, request.CurrentPassword);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize(Policy = ApiConstants.AuthPolicies.AdminOnly)]
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