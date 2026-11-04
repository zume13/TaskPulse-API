using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.Leave;

public record LeaveWorkspaceCommand(Guid WorkspaceId, Guid UserId) : IRequest<Result>;