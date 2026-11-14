using Api.Extensions;
using Application.Features.Notifications.Commands.MarkAllAsRead;
using Application.Features.Notifications.Commands.MarkAsRead;
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
    public async Task<IActionResult> GetMine(
        [FromQuery] bool unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(
            new GetMyNotificationsQuery(userId.Value, unreadOnly, page, pageSize),
            cancellationToken);

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
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(
            new MarkNotificationAsReadCommand(notificationId, userId.Value),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await sender.Send(new MarkAllNotificationsAsReadCommand(userId.Value), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }
}
