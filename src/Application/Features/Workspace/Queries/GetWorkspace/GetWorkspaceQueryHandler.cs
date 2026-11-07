using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetWorkspace;

public class GetWorkspaceQueryHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetWorkspaceQuery, ResultT<WorkspaceReadDto>>
{
    public async Task<ResultT<WorkspaceReadDto>> Handle(
        GetWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        var role = workspace?.GetMemberRole(request.UserId);

        // Match existing commands: inaccessible and nonexistent workspaces look identical.
        if (workspace is null || role is null)
            return WorkspaceErrors.WorkspaceNotFound;

        return new WorkspaceReadDto(
            workspace.Id,
            workspace.WorkspaceName.Value,
            role.Value,
            workspace.Members.Count,
            workspace.IsArchived);
    }
}
