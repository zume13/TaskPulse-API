using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.Archive;

public class ArchiveWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ArchiveWorkspaceCommand, Result>
{
    public async Task<Result> Handle(ArchiveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAndInvitationsAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null || !workspace.IsMember(request.ActorId))
            return WorkspaceErrors.WorkspaceNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var archiveResult = workspace.Archive(request.ActorId, nowUtc);

        if (archiveResult.IsFailure)
            return archiveResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}