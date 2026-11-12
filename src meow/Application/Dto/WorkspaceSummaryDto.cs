using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Dto;

public record WorkspaceSummaryDto(
    Guid Id,
    string Name,
    Guid CreatedBy,
    DateTime CreatedAtUtc,
    bool IsArchived,
    WorkspaceRole MyRole,
    int MemberCount);
