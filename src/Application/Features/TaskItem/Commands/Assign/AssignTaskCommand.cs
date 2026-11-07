using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Assign;

public record AssignTaskCommand(Guid TaskId, Guid UserId, Guid AssigneeId) : IRequest<Result>;