using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.User;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Users.Queries.GetAllUsersWithWorkspaces;

public class GetAllUsersWithWorkspacesQueryHandler(
    IUserRepository userRepository,
    IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetAllUsersWithWorkspacesQuery, ResultT<PagedResultDto<AdminUserDto>>>
{
    public async Task<ResultT<PagedResultDto<AdminUserDto>>> Handle(
        GetAllUsersWithWorkspacesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await userRepository.GetPagedAsync(
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        // One membership lookup for the whole page, not one per user.
        var userIds = result.Items.Select(user => user.Id).ToList();

        var membershipsByUser = (await workspaceRepository.GetMembershipsByUserIdsAsync(
                userIds,
                request.IncludeArchived,
                cancellationToken))
            .ToLookup(membership => membership.UserId);

        var items = result.Items
            .Select(user =>
            {
                var workspaces = membershipsByUser[user.Id]
                    .OrderBy(membership => membership.WorkspaceName)
                    .ThenBy(membership => membership.WorkspaceId)
                    .Select(membership => new UserWorkspaceDto(
                        membership.WorkspaceId,
                        membership.WorkspaceName,
                        membership.IsArchived,
                        membership.Role,
                        membership.JoinedAtUtc))
                    .ToList();

                return new AdminUserDto(
                    user.Id,
                    user.Email.Value,
                    user.FirstName.Value,
                    user.LastName.Value,
                    user.Role,
                    user.IsEmailVerified,
                    user.RegisteredAtUtc,
                    workspaces.Count,
                    workspaces);
            })
            .ToList();

        return new PagedResultDto<AdminUserDto>(items, request.Page, request.PageSize, result.TotalCount);
    }
}
