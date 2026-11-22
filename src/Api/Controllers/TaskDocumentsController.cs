using Api.Extensions;
using Application.Dto;
using Application.Features.Tasks.Documents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Authorize]
[Route("api/tasks/{taskId:guid}/documents")]
public class TaskDocumentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid taskId, CancellationToken ct)
    {
        var user = User.GetUserId(); if (user is null || user == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new ListTaskDocumentsQuery(taskId, user.Value), ct);
        return result.IsFailure ? result.ToProblemResult() : Ok(result.value);
    }
    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> Get(Guid taskId, Guid documentId, CancellationToken ct)
    {
        var user = User.GetUserId(); if (user is null || user == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new GetTaskDocumentQuery(taskId, documentId, user.Value), ct);
        return result.IsFailure ? result.ToProblemResult() : Ok(result.value);
    }
    [HttpPost]
    public async Task<IActionResult> Add(Guid taskId, TaskDocumentRequestDto request, CancellationToken ct)
    {
        var user = User.GetUserId(); if (user is null || user == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new AddTaskDocumentCommand(taskId, user.Value, request.Url, request.Label), ct);
        return result.IsFailure ? result.ToProblemResult() : CreatedAtAction(nameof(Get), new { taskId, documentId = result.value.Id }, result.value);
    }
    [HttpPatch("{documentId:guid}")]
    public async Task<IActionResult> Update(Guid taskId, Guid documentId, TaskDocumentRequestDto request, CancellationToken ct)
    {
        var user = User.GetUserId(); if (user is null || user == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new UpdateTaskDocumentCommand(taskId, documentId, user.Value, request.Url, request.Label), ct);
        return result.IsFailure ? result.ToProblemResult() : NoContent();
    }
    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Remove(Guid taskId, Guid documentId, CancellationToken ct)
    {
        var user = User.GetUserId(); if (user is null || user == Guid.Empty) return Unauthorized();
        var result = await sender.Send(new RemoveTaskDocumentCommand(taskId, documentId, user.Value), ct);
        return result.IsFailure ? result.ToProblemResult() : NoContent();
    }
}
