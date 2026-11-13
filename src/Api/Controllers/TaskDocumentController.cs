using Api.Extensions;
using Application.Dto;
using Application.Features.Tasks.Commands.RemoveDocumentLink;
using Application.Features.Tasks.Commands.SetDocumentLink;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/tasks/{taskId:guid}/document")]
public class TaskDocumentController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Set(Guid taskId, SetDocumentLinkRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new SetDocumentLinkCommand(taskId, userId.Value, request.Url, request.Label);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Remove(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new RemoveDocumentLinkCommand(taskId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }
}