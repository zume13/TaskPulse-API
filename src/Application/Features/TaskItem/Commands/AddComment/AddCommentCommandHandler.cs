using Application.Dto;
using Application.Features.Notifications;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.TaskComment.ValueObjects;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.AddComment;

public class AddCommentCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddCommentCommand, ResultT<CommentDto>>
{
    public async Task<ResultT<CommentDto>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdWithCommentsAsync(request.TaskId, cancellationToken);

        if (task is null)
            return TaskCommandErrors.TaskNotFound;

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(task.WorkspaceId, cancellationToken);

        if (workspace is null || !workspace.IsMember(request.UserId))
            return TaskCommandErrors.TaskNotFound;

        if (workspace.IsArchived)
            return WorkspaceErrors.WorkspaceDeletedOrArchived;

        var bodyResult = CommentBody.Create(request.Body);

        if (bodyResult.IsFailure)
            return bodyResult.Error;

        var addResult = task.AddComment(request.UserId, bodyResult.value);

        if (addResult.IsFailure)
            return addResult.Error;

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error;

        var comment = task.Comments.Last();

        var author = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        return new CommentDto(
            comment.Id,
            comment.AuthorId,
            UserNames.DisplayName(author),
            comment.Body.Value,
            comment.CreatedAtUtc,
            comment.UpdatedAtUtc);
    }
}