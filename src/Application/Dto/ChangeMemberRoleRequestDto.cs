using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Dto;

public record ChangeMemberRoleRequestDto(WorkspaceRole Role);