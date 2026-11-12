using Application.Dto;
using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Collaboration.Workspaces.Workspace.Errors;
using Domain.Aggregates.User;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Workspace.Queries.GetWorkspaceById;

public class GetWorkspaceByIdQueryHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository) : IRequestHandler<GetWorkspaceByIdQuery, ResultT<WorkspaceDetailsDto>>
{
    public async Task<ResultT<WorkspaceDetailsDto>> Handle(
        GetWorkspaceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);

        var myRole = workspace?.GetMemberRole(request.UserId);

        // Same answer for "does not exist" and "not a member".
        if (workspace is null || myRole is null)
            return WorkspaceErrors.WorkspaceNotFound;

        var memberIds = workspace.Members.Select(member => member.UserId).ToList();

        var users = (await userRepository.GetByIdsAsync(memberIds, cancellationToken))
            .ToDictionary(user => user.Id);

        IReadOnlyList<WorkspaceMemberDto> members = workspace.Members
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
            .ToList();

        return new WorkspaceDetailsDto(
            workspace.Id,
            workspace.WorkspaceName.Value,
            workspace.CreatedBy,
            workspace.CreatedAtUtc,
            workspace.IsArchived,
            workspace.ArchivedAtUtc,
            myRole.Value,
            members);
    }
}
