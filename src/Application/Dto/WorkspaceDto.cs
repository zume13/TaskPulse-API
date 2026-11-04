namespace Application.Dto;

public record WorkspaceDto(Guid Id, string Name, Guid CreatedBy, DateTime CreatedAtUtc, bool IsArchived);