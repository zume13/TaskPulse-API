using Application.Dto;
using Domain.Aggregates.Notifications;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler(
    INotificationRepository notificationRepository) : IRequestHandler<GetUnreadCountQuery, ResultT<UnreadCountDto>>
{
    public async Task<ResultT<UnreadCountDto>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await notificationRepository.CountUnreadAsync(request.UserId, cancellationToken);

        return new UnreadCountDto(count);
    }
}