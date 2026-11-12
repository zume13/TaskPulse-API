using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetWorkspaces;

public class GetWorkspacesQueryHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetWorkspacesQuery, ResultT<List<WorkspaceReadDto>>>
{
    public async Task<ResultT<List<WorkspaceReadDto>>> Handle(
        GetWorkspacesQuery request, CancellationToken cancellationToken)
    {
        // The repository loads memberships with the workspaces, not once per result.
        var workspaces = await workspaceRepository.GetByMemberIdAsync(request.UserId, cancellationToken);

        return workspaces
            .Where(workspace => workspace.IsMember(request.UserId))
            .Select(workspace => new WorkspaceReadDto(
                workspace.Id,
                workspace.WorkspaceName.Value,
                workspace.GetMemberRole(request.UserId)!.Value,
                workspace.Members.Count,
                workspace.IsArchived))
            .ToList();
    }
}
