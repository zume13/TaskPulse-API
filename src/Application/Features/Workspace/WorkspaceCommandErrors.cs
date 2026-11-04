using Domain.Shared.ResultPattern;

namespace Application.Features.Workspace;

public static class WorkspaceCommandErrors
{
    public static Error InvitedUserNotFound => Error.NotFound("Workspace.InvitedUserNotFound", "No account exists with that email address.");
}