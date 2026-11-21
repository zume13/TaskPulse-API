using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.WorkspaceDocuments;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Commands.Rename;

public class RenameWorkspaceDocumentCommandHandler(IWorkspaceRepository workspaceRepository,
    IWorkspaceDocumentRepository documentRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<RenameWorkspaceDocumentCommand, Result>
{
    public async Task<Result> Handle(RenameWorkspaceDocumentCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        var role = workspace?.GetMemberRole(request.UserId);
        if (workspace is null || role is null) return WorkspaceErrors.WorkspaceNotFound;

        var document = await documentRepository.GetInWorkspaceAsync(request.WorkspaceId, request.DocumentId, cancellationToken);
        if (document is null) return WorkspaceDocumentErrors.NotFound;
        if (workspace.IsArchived) return WorkspaceErrors.WorkspaceDeletedOrArchived;
        if (document.CreatedByUserId != request.UserId && role is not (WorkspaceRole.Owner or WorkspaceRole.Admin))
            return WorkspaceDocumentErrors.NotAllowed;

        var renamed = document.Rename(request.Title, request.UserId, timeProvider.GetUtcNow().UtcDateTime);
        if (renamed.IsFailure) return renamed;
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
