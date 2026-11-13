using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAllRead;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Result>;