using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Aggregates.Tasks.TaskComment.ValueObjects;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.EditComment;

public class EditCommentCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<EditCommentCommand, Result>
{
    public async Task<Result> Handle(EditCommentCommand request, CancellationToken cancellationToken)
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

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var editResult = task.EditComment(request.CommentId, request.UserId, bodyResult.value, nowUtc);

        if (editResult.IsFailure)
            return editResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}