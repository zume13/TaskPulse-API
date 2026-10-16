using Domain.Aggregates.User.Enum;
using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.ChangeRole;

public record ChangeRoleCommand(Guid UserId, Role NewRole) : IRequest<Result>;