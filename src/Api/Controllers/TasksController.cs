using Api.Extensions;
using Application.Dto;
using Application.Features.Tasks.Commands.Assign;
using Application.Features.Tasks.Commands.Complete;
using Application.Features.Tasks.Commands.Create;
using Application.Features.Tasks.Commands.Delete;
using Application.Features.Tasks.Commands.Reopen;
using Application.Features.Tasks.Commands.Start;
using Application.Features.Tasks.Commands.Unassign;
using Application.Features.Tasks.Commands.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public class TasksController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost("workspaces/{workspaceId:guid}/tasks")]
    public async Task<IActionResult> Create(Guid workspaceId, CreateTaskRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new CreateTaskCommand(
            workspaceId,
            userId.Value,
            request.Title,
            request.DueDate,
            request.Priority,
            request.Description);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return StatusCode(StatusCodes.Status201Created, result.value);
    }

    [Authorize]
    [HttpPut("tasks/{taskId:guid}")]
    public async Task<IActionResult> Update(Guid taskId, UpdateTaskRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new UpdateTaskCommand(
            taskId,
            userId.Value,
            request.Title,
            request.DueDate,
            request.Priority,
            request.Description);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new DeleteTaskCommand(taskId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("tasks/{taskId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid taskId, AssignTaskRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new AssignTaskCommand(taskId, userId.Value, request.AssigneeId), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("tasks/{taskId:guid}/assignee")]
    public async Task<IActionResult> Unassign(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new UnassignTaskCommand(taskId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("tasks/{taskId:guid}/start")]
    public async Task<IActionResult> Start(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new StartTaskCommand(taskId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("tasks/{taskId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new CompleteTaskCommand(taskId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("tasks/{taskId:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new ReopenTaskCommand(taskId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }
}