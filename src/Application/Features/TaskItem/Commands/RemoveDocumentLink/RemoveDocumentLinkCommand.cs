using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.RemoveDocumentLink;

public record RemoveDocumentLinkCommand(Guid TaskId, Guid UserId) : IRequest<Result>;