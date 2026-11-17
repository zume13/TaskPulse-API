using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Dto;

/// <summary>One workspace a user belongs to, with the user's role in it.</summary>
public record UserWorkspaceDto(
    Guid WorkspaceId,
    string Name,
    bool IsArchived,
    WorkspaceRole Role,
    DateTime JoinedAtUtc);
