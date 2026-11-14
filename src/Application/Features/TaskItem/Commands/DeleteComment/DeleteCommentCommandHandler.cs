using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Tasks.Commands.DeleteComment;

public class DeleteCommentCommandHandler(
    ITaskItemRepository taskRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<DeleteCommentCommand, Result>
{
    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdWithCommentsAsync(request.TaskId, cancellationToken);

        if (task is null)
            return TaskCommandErrors.TaskNotFound;

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(task.WorkspaceId, cancellationToken);

        if (workspace is null || !workspace.IsMember(request.UserId))
            return TaskCommandErrors.TaskNotFound;

        if (workspace.IsArchived)
            return WorkspaceErrors.WorkspaceDeletedOrArchived;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Soft delete; only the author can delete (the domain checks it).
        var deleteResult = task.DeleteComment(request.CommentId, request.UserId, nowUtc);

        if (deleteResult.IsFailure)
            return deleteResult.Error;

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
