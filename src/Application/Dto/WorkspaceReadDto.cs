using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Dto;

public record WorkspaceReadDto(
    Guid Id,
    string Name,
    WorkspaceRole MyRole,
    int MemberCount,
    bool IsArchived);
