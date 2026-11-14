using Application.Dto;
using Domain.Aggregates.Notifications;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAllAsRead;

public class MarkAllNotificationsAsReadCommandHandler(
    INotificationRepository notificationRepository,
    TimeProvider timeProvider) : IRequestHandler<MarkAllNotificationsAsReadCommand, ResultT<MarkedCountDto>>
{
    public async Task<ResultT<MarkedCountDto>> Handle(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Set-based update: runs immediately and does not go through the UnitOfWork.
        var marked = await notificationRepository.MarkAllAsReadAsync(request.UserId, nowUtc, cancellationToken);

        return new MarkedCountDto(marked);
    }
}
