using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Queries.List;

public record ListWorkspaceDocumentsQuery(Guid WorkspaceId, Guid UserId) : IRequest<ResultT<IReadOnlyList<WorkspaceDocumentDto>>>;
