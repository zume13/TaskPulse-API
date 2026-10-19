using Domain.Aggregates.User.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.ChangeRole;

public record ChangeRoleCommand(Guid UserId, Role NewRole) : IRequest<Result>;