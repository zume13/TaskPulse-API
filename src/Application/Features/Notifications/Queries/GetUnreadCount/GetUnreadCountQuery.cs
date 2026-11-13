using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery(Guid UserId) : IRequest<ResultT<UnreadCountDto>>;