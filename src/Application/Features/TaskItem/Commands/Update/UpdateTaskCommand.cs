using Domain.Aggregates.Tasks.Task.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Update;

public record UpdateTaskCommand(Guid TaskId, Guid UserId, string Title, DateTimeOffset DueDate, Priority Priority, string? Description) : IRequest<Result>;