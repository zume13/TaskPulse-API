using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Dto;

public record InviteMemberRequestDto(string Email, WorkspaceRole Role);