using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.Archive;

public record ArchiveWorkspaceCommand(Guid WorkspaceId, Guid ActorId) : IRequest<Result>;