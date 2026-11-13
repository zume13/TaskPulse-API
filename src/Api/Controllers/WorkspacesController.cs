using Api.Extensions;
using Application.Dto;
using Application.Features.Workspace.Commands.AcceptInvitation;
using Application.Features.Workspace.Commands.Archive;
using Application.Features.Workspace.Commands.ChangeMemberRole;
using Application.Features.Workspace.Commands.Create;
using Application.Features.Workspace.Commands.InviteMember;
using Application.Features.Workspace.Commands.Leave;
using Application.Features.Workspace.Commands.RemoveMember;
using Application.Features.Workspace.Commands.Rename;
using Application.Features.Workspace.Commands.TransferOwnership;
using Application.Features.Workspace.Queries.GetMyWorkspaces;
using Application.Features.Workspace.Queries.GetWorkspaceById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public class WorkspacesController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkspaceRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new CreateWorkspaceCommand(userId.Value, request.Name);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return StatusCode(StatusCodes.Status201Created, result.value);
    }

    [Authorize]
    [HttpPut("{workspaceId:guid}/name")]
    public async Task<IActionResult> Rename(Guid workspaceId, RenameWorkspaceRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new RenameWorkspaceCommand(workspaceId, userId.Value, request.Name);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("{workspaceId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new ArchiveWorkspaceCommand(workspaceId, userId.Value);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("{workspaceId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new LeaveWorkspaceCommand(workspaceId, userId.Value);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("{workspaceId:guid}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(Guid workspaceId, TransferOwnershipRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new TransferOwnershipCommand(workspaceId, userId.Value, request.NewOwnerId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("{workspaceId:guid}/invitations")]
    public async Task<IActionResult> InviteMember(Guid workspaceId, InviteMemberRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new InviteMemberCommand(workspaceId, userId.Value, request.Email, request.Role);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("invitations/{invitationId:guid}/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid invitationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new AcceptInvitationCommand(invitationId, userId.Value);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPut("{workspaceId:guid}/members/{memberId:guid}/role")]
    public async Task<IActionResult> ChangeMemberRole(Guid workspaceId, Guid memberId, ChangeMemberRoleRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new ChangeMemberRoleCommand(workspaceId, userId.Value, memberId, request.Role);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{workspaceId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid memberId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new RemoveMemberCommand(workspaceId, userId.Value, memberId);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool includeArchived, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new GetMyWorkspacesQuery(userId.Value, includeArchived), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }

    [Authorize]
    [HttpGet("{workspaceId:guid}")]
    public async Task<IActionResult> GetById(Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new GetWorkspaceByIdQuery(workspaceId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }
}