using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Reopen;

public record ReopenTaskCommand(Guid TaskId, Guid UserId) : IRequest<Result>;