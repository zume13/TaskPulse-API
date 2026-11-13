using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.DeleteComment;

public record DeleteCommentCommand(Guid TaskId, Guid CommentId, Guid UserId) : IRequest<Result>;