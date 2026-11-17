using Domain.Aggregates.User.Enums;

namespace Application.Dto;

/// <summary>A user with every workspace they belong to (empty list if none). Admin view.</summary>
public record AdminUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Role Role,
    bool IsEmailVerified,
    DateTime RegisteredAtUtc,
    int WorkspaceCount,
    IReadOnlyList<UserWorkspaceDto> Workspaces);
