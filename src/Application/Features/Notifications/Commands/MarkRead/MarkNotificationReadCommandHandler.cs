using Domain.Aggregates.Notifications;
using Domain.Aggregates.Notifications.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAsRead;

public class MarkNotificationAsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        // Someone else's notification looks the same as a missing one.
        if (notification is null || notification.RecipientId != request.UserId)
            return NotificationErrors.NotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var markResult = notification.MarkAsRead(request.UserId, nowUtc);

        if (markResult.IsFailure)
            return markResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
