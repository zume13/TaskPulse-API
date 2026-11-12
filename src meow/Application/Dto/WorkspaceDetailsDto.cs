using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Dto;

public record WorkspaceDetailsDto(
    Guid Id,
    string Name,
    Guid CreatedBy,
    DateTime CreatedAtUtc,
    bool IsArchived,
    DateTime? ArchivedAtUtc,
    WorkspaceRole MyRole,
    IReadOnlyList<WorkspaceMemberDto> Members);
