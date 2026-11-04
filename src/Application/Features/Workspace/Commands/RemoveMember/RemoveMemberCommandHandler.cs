using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Commands.RemoveMember;

public class RemoveMemberCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RemoveMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAndInvitationsAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null || !workspace.IsMember(request.ActorId))
            return WorkspaceErrors.WorkspaceNotFound;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var removeResult = workspace.RemoveMember(request.ActorId, request.MemberId, nowUtc);

        if (removeResult.IsFailure)
            return removeResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}