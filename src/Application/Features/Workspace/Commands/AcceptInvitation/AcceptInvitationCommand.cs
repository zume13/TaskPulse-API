using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.AcceptInvitation;

public record AcceptInvitationCommand(Guid InvitationId, Guid UserId) : IRequest<Result>;