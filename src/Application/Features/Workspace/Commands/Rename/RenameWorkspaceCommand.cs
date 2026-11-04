using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.Rename;

public record RenameWorkspaceCommand(Guid WorkspaceId, Guid ActorId, string Name) : IRequest<Result>;