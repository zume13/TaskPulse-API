using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkRead;

public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<Result>;