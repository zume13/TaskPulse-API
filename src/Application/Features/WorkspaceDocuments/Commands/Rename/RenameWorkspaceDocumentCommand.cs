using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Commands.Rename;

public record RenameWorkspaceDocumentCommand(Guid WorkspaceId, Guid DocumentId, Guid UserId, string Title) : IRequest<Result>;
