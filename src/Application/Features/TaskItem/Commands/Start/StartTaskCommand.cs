using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Start;

public record StartTaskCommand(Guid TaskId, Guid UserId) : IRequest<Result>;