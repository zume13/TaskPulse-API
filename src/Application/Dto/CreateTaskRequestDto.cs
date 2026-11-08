using Domain.Aggregates.Tasks.Task.Enums;

namespace Application.Dto;

public record CreateTaskRequestDto(
    string Title,
    DateTimeOffset DueDate,
    Priority Priority = Priority.Medium,
    string? Description = null);