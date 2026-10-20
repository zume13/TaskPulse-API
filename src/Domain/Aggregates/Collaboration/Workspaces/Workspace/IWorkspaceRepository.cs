using Domain.Shared.Base;

namespace Domain.Aggregates.Collaboration.Workspaces.Workspace
{

    public interface IWorkspaceRepository : IRepository<Workspace>
    {
        Task<List<Workspace>> GetByMemberIdAsync(
            Guid userId,
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
    }
}
