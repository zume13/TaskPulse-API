using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.WorkspaceDocuments;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Commands.Remove;

public class RemoveWorkspaceDocumentCommandHandler(IWorkspaceRepository workspaceRepository,
    IWorkspaceDocumentRepository documentRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveWorkspaceDocumentCommand, Result>
{
    public async Task<Result> Handle(RemoveWorkspaceDocumentCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        var role = workspace?.GetMemberRole(request.UserId);
        if (workspace is null || role is null) return WorkspaceErrors.WorkspaceNotFound;

        var document = await documentRepository.GetInWorkspaceAsync(request.WorkspaceId, request.DocumentId, cancellationToken);
        if (document is null) return WorkspaceDocumentErrors.NotFound;
        if (workspace.IsArchived) return WorkspaceErrors.WorkspaceDeletedOrArchived;
        if (document.CreatedByUserId != request.UserId && role is not (WorkspaceRole.Owner or WorkspaceRole.Admin))
            return WorkspaceDocumentErrors.NotAllowed;

        documentRepository.Remove(document);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
