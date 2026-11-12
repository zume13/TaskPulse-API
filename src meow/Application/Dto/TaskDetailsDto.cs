using Domain.Aggregates.Tasks.Task.Enums;

namespace Application.Dto;

public record TaskDetailsDto(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string? Description,
    DateTime DueDate,
    Status Status,
    Priority Priority,
    Guid? AssignedTo,
    string? AssignedToName,
    Guid CreatedBy,
    string CreatedByName,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DocumentLinkDto? LinkedDocument,
    IReadOnlyList<CommentDto> Comments);
