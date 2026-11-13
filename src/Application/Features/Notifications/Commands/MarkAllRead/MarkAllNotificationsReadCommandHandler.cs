using Domain.Aggregates.Notifications;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAllRead;

public class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    TimeProvider timeProvider) : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await notificationRepository.MarkAllAsReadAsync(request.UserId, nowUtc, cancellationToken);

        return Result.Success();
    }
}