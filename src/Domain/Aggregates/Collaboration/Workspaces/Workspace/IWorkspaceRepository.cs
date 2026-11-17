using Domain.Aggregates.Collaboration.Workspaces.Member.Enums;
using Domain.Shared.Base;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace
{
    public sealed record WorkspaceSearchResult(IReadOnlyList<Workspace> Items, int TotalCount);

    public sealed record UserWorkspaceMembership(
        Guid UserId,
        Guid WorkspaceId,
        string WorkspaceName,
        bool IsArchived,
        WorkspaceRole Role,
        DateTime JoinedAtUtc);

    public interface IWorkspaceRepository : IRepository<Workspace>
    {
        Task<WorkspaceSearchResult> GetAllWithMembersAsync(
            bool includeArchived,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

        Task<List<UserWorkspaceMembership>> GetMembershipsByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            bool includeArchived,
            CancellationToken cancellationToken = default);

        Task<List<Workspace>> GetByMemberIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<List<Workspace>> GetByMemberIdWithMembersAsync(
            Guid userId,
            bool includeArchived,
            CancellationToken cancellationToken = default);

        Task<Workspace?> GetByInvitationIdAsync(
            Guid invitationId,
            CancellationToken cancellationToken = default);

        Task<Workspace?> GetByIdWithMembersAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default);

        Task<Workspace?> GetByIdWithInvitationsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default);

        Task<Workspace?> GetByIdWithMembersAndInvitationsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default);
    }
}
