using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.WorkspaceDocuments.Commands.Remove;

public record RemoveWorkspaceDocumentCommand(Guid WorkspaceId, Guid DocumentId, Guid UserId) : IRequest<Result>;
