using Api.Extensions;
using Application.Dto;
using Application.Features.Tasks.Commands.AddComment;
using Application.Features.Tasks.Commands.DeleteComment;
using Application.Features.Tasks.Commands.EditComment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/tasks/{taskId:guid}/comments")]
public class TaskCommentsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add(Guid taskId, CommentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new AddCommentCommand(taskId, userId.Value, request.Body), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return StatusCode(StatusCodes.Status201Created, result.value);
    }

    [Authorize]
    [HttpPut("{commentId:guid}")]
    public async Task<IActionResult> Edit(Guid taskId, Guid commentId, CommentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new EditCommentCommand(taskId, commentId, userId.Value, request.Body), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId, Guid commentId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new DeleteCommentCommand(taskId, commentId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }
}