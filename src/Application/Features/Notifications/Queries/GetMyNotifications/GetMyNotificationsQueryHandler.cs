using Application.Dto;
using Domain.Aggregates.Notifications;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler(
    INotificationRepository notificationRepository) : IRequestHandler<GetMyNotificationsQuery, ResultT<NotificationListDto>>
{
    public async Task<ResultT<NotificationListDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var notifications = await notificationRepository.GetByRecipientAsync(
            request.UserId,
            request.UnreadOnly,
            skip,
            request.PageSize + 1,
            cancellationToken);

        var hasMore = notifications.Count > request.PageSize;

        var items = notifications
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.WorkspaceId,
                n.TargetType,
                n.TargetId,
                n.IsRead,
                n.CreatedAtUtc,
                n.ReadAtUtc))
            .ToList();

        return new NotificationListDto(items, request.Page, request.PageSize, hasMore);
    }
}