using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.WorkspaceDocuments;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Queries.List;

public class ListWorkspaceDocumentsQueryHandler(IWorkspaceRepository workspaceRepository,
    IWorkspaceDocumentRepository documentRepository)
    : IRequestHandler<ListWorkspaceDocumentsQuery, ResultT<IReadOnlyList<WorkspaceDocumentDto>>>
{
    public async Task<ResultT<IReadOnlyList<WorkspaceDocumentDto>>> Handle(
        ListWorkspaceDocumentsQuery request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null || !workspace.IsMember(request.UserId))
            return WorkspaceErrors.WorkspaceNotFound;

        var documents = await documentRepository.ListForWorkspaceAsync(request.WorkspaceId, cancellationToken);
        IReadOnlyList<WorkspaceDocumentDto> response = documents.Select(WorkspaceDocumentDto.From).ToList();
        return Result.Success(response);
    }
}
