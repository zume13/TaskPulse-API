using Api.Extensions;
using Application.Dto;
using Application.Features.Notifications.Commands.MarkAllRead;
using Application.Features.Notifications.Commands.MarkRead;
using Application.Features.Notifications.Queries.GetMyNotifications;
using Application.Features.Notifications.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] NotificationListRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var query = new GetMyNotificationsQuery(userId.Value, request.UnreadOnly, request.Page, request.PageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }

    [Authorize]
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new GetUnreadCountQuery(userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }

    [Authorize]
    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new MarkNotificationReadCommand(notificationId, userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new MarkAllNotificationsReadCommand(userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }
}