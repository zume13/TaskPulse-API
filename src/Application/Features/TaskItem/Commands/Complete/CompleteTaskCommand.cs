using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Complete;

public record CompleteTaskCommand(Guid TaskId, Guid UserId) : IRequest<Result>;