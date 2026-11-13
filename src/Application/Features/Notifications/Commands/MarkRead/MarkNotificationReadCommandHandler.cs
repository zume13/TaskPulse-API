using Domain.Aggregates.Notifications;
using Domain.Aggregates.Notifications.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkRead;

public class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null || notification.RecipientId != request.UserId)
            return NotificationErrors.NotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var markResult = notification.MarkAsRead(request.UserId, nowUtc);

        if (markResult.IsFailure)
            return markResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}