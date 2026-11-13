using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(Guid UserId, bool UnreadOnly, int Page, int PageSize) : IRequest<ResultT<NotificationListDto>>;