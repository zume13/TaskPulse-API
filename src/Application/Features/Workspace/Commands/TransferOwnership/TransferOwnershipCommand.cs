using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.TransferOwnership;

public record TransferOwnershipCommand(Guid WorkspaceId, Guid ActorId, Guid NewOwnerId) : IRequest<Result>;