using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.EditComment;

public record EditCommentCommand(Guid TaskId, Guid CommentId, Guid UserId, string Body) : IRequest<Result>;