using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Unassign;

public record UnassignTaskCommand(Guid TaskId, Guid UserId) : IRequest<Result>;