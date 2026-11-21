using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.WorkspaceDocuments;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Commands.Register;

public class RegisterWorkspaceDocumentCommandHandler(IWorkspaceRepository workspaceRepository,
    IWorkspaceDocumentRepository documentRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<RegisterWorkspaceDocumentCommand, ResultT<WorkspaceDocumentDto>>
{
    public async Task<ResultT<WorkspaceDocumentDto>> Handle(RegisterWorkspaceDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null || !workspace.IsMember(request.UserId))
            return WorkspaceErrors.WorkspaceNotFound;
        if (workspace.IsArchived)
            return WorkspaceErrors.WorkspaceDeletedOrArchived;

        if (await documentRepository.ExistsAsync(request.WorkspaceId, request.GoogleFileId, cancellationToken))
            return WorkspaceDocumentErrors.Duplicate;

        var created = WorkspaceDocument.Create(request.WorkspaceId, request.Title, request.GoogleFileId,
            request.SourceType, request.MimeType, request.UserId, timeProvider.GetUtcNow().UtcDateTime);
        if (created.IsFailure) return created.Error;

        await documentRepository.AddAsync(created.value, cancellationToken);
        // The unique database index is authoritative if another request passed the precheck too.
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return saved.Error;

        return WorkspaceDocumentDto.From(created.value);
    }
}
