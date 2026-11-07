using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;

namespace Application.Features.Tasks;

public static class TaskPermissions
{
    public static bool IsManager(WorkspaceRole role) => role is WorkspaceRole.Owner or WorkspaceRole.Admin;
}