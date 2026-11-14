using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAsRead;

public record MarkNotificationAsReadCommand(Guid NotificationId, Guid UserId) : IRequest<Result>;
