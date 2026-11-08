using Application.Dto;
using Domain.Aggregates.Tasks.Task.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.Create;

public record CreateTaskCommand(
    Guid WorkspaceId, Guid UserId, string Title, DateTimeOffset DueDate, Priority Priority,
    string? Description) : IRequest<ResultT<TaskDto>>;