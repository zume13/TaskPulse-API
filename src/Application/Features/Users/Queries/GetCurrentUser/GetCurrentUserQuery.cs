using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<ResultT<UserDto>>;