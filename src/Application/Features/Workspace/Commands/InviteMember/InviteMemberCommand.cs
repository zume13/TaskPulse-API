using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.InviteMember;

public record InviteMemberCommand(Guid WorkspaceId, Guid ActorId, string Email, WorkspaceRole Role) : IRequest<Result>;