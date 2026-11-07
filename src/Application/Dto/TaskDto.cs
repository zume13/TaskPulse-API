using Domain.Aggregates.Tasks.Task.Enums;

namespace Application.Dto;

public record TaskDto(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string? Description,
    DateTime DueDate,
    Status Status,
    Priority Priority,
    Guid? AssignedTo,
    Guid CreatedBy,
    DateTime CreatedAtUtc);