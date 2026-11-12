using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetMyWorkspaces;

public class GetMyWorkspacesQueryHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetMyWorkspacesQuery, ResultT<IReadOnlyList<WorkspaceSummaryDto>>>
{
    public async Task<ResultT<IReadOnlyList<WorkspaceSummaryDto>>> Handle(
        GetMyWorkspacesQuery request,
        CancellationToken cancellationToken)
    {
        var workspaces = await workspaceRepository.GetByMemberIdWithMembersAsync(
            request.UserId,
            request.IncludeArchived,
            cancellationToken);

        IReadOnlyList<WorkspaceSummaryDto> dtos = workspaces
            .Select(workspace => new WorkspaceSummaryDto(
                workspace.Id,
                workspace.WorkspaceName.Value,
                workspace.CreatedBy,
                workspace.CreatedAtUtc,
                workspace.IsArchived,
                workspace.GetMemberRole(request.UserId)!.Value,
                workspace.Members.Count))
            .ToList();

        return Result.Success(dtos);
    }
}
