using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Commands.MarkAllAsRead;

public record MarkAllNotificationsAsReadCommand(Guid UserId) : IRequest<ResultT<MarkedCountDto>>;
