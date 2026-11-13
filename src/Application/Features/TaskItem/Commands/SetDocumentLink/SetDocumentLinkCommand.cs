using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.SetDocumentLink;

public record SetDocumentLinkCommand(Guid TaskId, Guid UserId, string Url, string Label) : IRequest<Result>;