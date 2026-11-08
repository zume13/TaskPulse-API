using Domain.Aggregates.Tasks.Task.Enums;

namespace Application.Dto;

public record UpdateTaskRequestDto(
    string Title,
    DateTimeOffset DueDate,
    Priority Priority,
    string? Description);