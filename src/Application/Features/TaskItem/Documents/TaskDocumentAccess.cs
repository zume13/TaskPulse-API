using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.Tasks;
using Domain.Aggregates.Tasks.Task;
using Domain.Shared.ResultPattern;

namespace Application.Features.Tasks.Documents;

internal static class TaskDocumentAccess
{
    public static async Task<ResultT<TaskItem>> Load(ITaskItemRepository tasks, IWorkspaceRepository workspaces,
        Guid taskId, Guid userId, bool write, CancellationToken ct)
    {
        var task = await tasks.GetByIdWithDocumentsAsync(taskId, ct);
        if (task is null || task.IsDeleted) return TaskCommandErrors.TaskNotFound;
        var workspace = await workspaces.GetByIdWithMembersAsync(task.WorkspaceId, ct);
        var role = workspace?.GetMemberRole(userId);
        if (workspace is null || role is null) return TaskCommandErrors.TaskNotFound;
        if (write && workspace.IsArchived) return WorkspaceErrors.WorkspaceDeletedOrArchived;
        if (write && task.CreatedBy != userId && task.AssignedTo != userId && !TaskPermissions.IsManager(role.Value))
            return TaskCommandErrors.NotAllowed;
        return task;
    }
}
