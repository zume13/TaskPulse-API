using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.User;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetAllWorkspaces;

public class GetAllWorkspacesQueryHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetAllWorkspacesQuery, ResultT<PagedResultDto<AdminWorkspaceDto>>>
{
    public async Task<ResultT<PagedResultDto<AdminWorkspaceDto>>> Handle(
        GetAllWorkspacesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await workspaceRepository.GetAllWithMembersAsync(
            request.IncludeArchived,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        // One user lookup for the whole page, not one per workspace.
        var memberIds = result.Items
            .SelectMany(workspace => workspace.Members)
            .Select(member => member.UserId)
            .Distinct()
            .ToList();

        var users = (await userRepository.GetByIdsAsync(memberIds, cancellationToken))
            .ToDictionary(user => user.Id);

        var items = result.Items
            .Select(workspace => new AdminWorkspaceDto(
                workspace.Id,
                workspace.WorkspaceName.Value,
                workspace.CreatedBy,
                workspace.CreatedAtUtc,
                workspace.IsArchived,
                workspace.ArchivedAtUtc,
                workspace.Members.Count,
                workspace.Members
                    .OrderBy(member => member.Role)
                    .ThenBy(member => member.JoinedAtUtc)
                    .Select(member =>
                    {
                        users.TryGetValue(member.UserId, out var user);

                        return new WorkspaceMemberDto(
                            member.UserId,
                            user?.FirstName.Value ?? "Unknown",
                            user?.LastName.Value ?? "user",
                            user?.Email.Value ?? string.Empty,
                            member.Role,
                            member.JoinedAtUtc);
                    })
                    .ToList()))
            .ToList();

        return new PagedResultDto<AdminWorkspaceDto>(items, request.Page, request.PageSize, result.TotalCount);
    }
}
