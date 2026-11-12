using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Dto;

/// <summary>UserId is also the id the member endpoints (role change, remove) expect.</summary>
public record WorkspaceMemberDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    WorkspaceRole Role,
    DateTime JoinedAtUtc);
