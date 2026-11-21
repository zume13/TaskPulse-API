using Application.Dto;
using Domain.Aggregates.WorkspaceDocuments;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Commands.Register;

public record RegisterWorkspaceDocumentCommand(Guid WorkspaceId, Guid UserId, string Title,
    string GoogleFileId, WorkspaceDocumentSource SourceType, string? MimeType) : IRequest<ResultT<WorkspaceDocumentDto>>;
