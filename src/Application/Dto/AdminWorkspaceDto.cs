namespace Application.Dto;

/// <summary>A workspace with all of its members. Admin view, not scoped to the caller.</summary>
public record AdminWorkspaceDto(
    Guid Id,
    string Name,
    Guid CreatedBy,
    DateTime CreatedAtUtc,
    bool IsArchived,
    DateTime? ArchivedAtUtc,
    int MemberCount,
    IReadOnlyList<WorkspaceMemberDto> Members);
