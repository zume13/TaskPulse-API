using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.ChangeMemberRole;

public record ChangeMemberRoleCommand(Guid WorkspaceId, Guid ActorId, Guid MemberId, WorkspaceRole NewRole) : IRequest<Result>;