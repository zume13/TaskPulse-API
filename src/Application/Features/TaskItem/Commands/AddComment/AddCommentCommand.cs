using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.AddComment;

public record AddCommentCommand(Guid TaskId, Guid UserId, string Body) : IRequest<ResultT<CommentDto>>;