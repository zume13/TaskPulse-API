using Application.Dto;
using Domain.Aggregates.Notifications;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler(INotificationRepository notificationRepository)
    : IRequestHandler<GetMyNotificationsQuery, ResultT<NotificationPageDto>>
{
    public async Task<ResultT<NotificationPageDto>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        // Ask for one extra row to learn whether another page exists, without a COUNT query.
        var notifications = await notificationRepository.GetByRecipientAsync(
            request.UserId,
            request.UnreadOnly,
            (request.Page - 1) * request.PageSize,
            request.PageSize + 1,
            cancellationToken);

        var hasMore = notifications.Count > request.PageSize;

        IReadOnlyList<NotificationDto> items = notifications
            .Take(request.PageSize)
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.WorkspaceId,
                notification.TargetType,
                notification.TargetId,
                notification.IsRead,
                notification.CreatedAtUtc,
                notification.ReadAtUtc))
            .ToList();

        var unreadCount = await notificationRepository.CountUnreadAsync(request.UserId, cancellationToken);

        return new NotificationPageDto(items, request.Page, request.PageSize, hasMore, unreadCount);
    }
}
