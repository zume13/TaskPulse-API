using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Delete;

public record DeleteTaskCommand(Guid TaskId, Guid UserId) : IRequest<Result>;