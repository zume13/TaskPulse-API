using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.RemoveMember;

public record RemoveMemberCommand(Guid WorkspaceId, Guid ActorId, Guid MemberId) : IRequest<Result>;