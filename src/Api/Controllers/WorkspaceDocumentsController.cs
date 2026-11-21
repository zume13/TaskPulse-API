using Api.Extensions;
using Application.Dto;
using Application.Features.WorkspaceDocuments.Commands.Register;
using Application.Features.WorkspaceDocuments.Commands.Remove;
using Application.Features.WorkspaceDocuments.Commands.Rename;
using Application.Features.WorkspaceDocuments.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/documents")]
public class WorkspaceDocumentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null || userId == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new ListWorkspaceDocumentsQuery(workspaceId, userId.Value), cancellationToken);
        if (result.IsFailure) return result.ToProblemResult();
        return Ok(result.value);
    }

    [HttpPost]
    public async Task<IActionResult> Register(Guid workspaceId, RegisterWorkspaceDocumentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null || userId == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new RegisterWorkspaceDocumentCommand(workspaceId, userId.Value,
            request.Title, request.GoogleFileId, request.SourceType, request.MimeType), cancellationToken);
        if (result.IsFailure) return result.ToProblemResult();
        return CreatedAtAction(nameof(List), new { workspaceId }, result.value);
    }

    [HttpPatch("{documentId:guid}")]
    public async Task<IActionResult> Rename(Guid workspaceId, Guid documentId, RenameWorkspaceDocumentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null || userId == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new RenameWorkspaceDocumentCommand(workspaceId, documentId, userId.Value, request.Title), cancellationToken);
        if (result.IsFailure) return result.ToProblemResult();
        return NoContent();
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Remove(Guid workspaceId, Guid documentId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null || userId == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new RemoveWorkspaceDocumentCommand(workspaceId, documentId, userId.Value), cancellationToken);
        if (result.IsFailure) return result.ToProblemResult();
        return NoContent();
    }
}
