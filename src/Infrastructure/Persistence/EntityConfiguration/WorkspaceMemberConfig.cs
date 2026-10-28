using Domain.Aggregates.Collaboration.Workspaces.Member;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class WorkspaceMemberConfig : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("WorkspaceMembers");

        builder.HasKey(x => x.Id);
        // Required: the domain generates the Id, and EF must treat new
        // members added to a loaded Workspace as INSERTs, not UPDATEs.
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Shadow FK. The relationship itself is configured in WorkspaceConfig.
        builder.Property<Guid>("WorkspaceId");

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.JoinedAtUtc).IsRequired();

        // A user can be a member of a workspace only once.
        builder.HasIndex("WorkspaceId", nameof(WorkspaceMember.UserId))
            .IsUnique();

        // Supports IWorkspaceRepository.GetByMemberIdAsync.
        builder.HasIndex(x => x.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}