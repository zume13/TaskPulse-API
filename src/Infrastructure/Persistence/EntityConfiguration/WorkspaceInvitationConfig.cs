using Domain.Aggregates.Collaboration.Workspaces.Invitations;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class WorkspaceInvitationConfig : IEntityTypeConfiguration<WorkspaceInvitation>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvitation> builder)
    {
        builder.ToTable("WorkspaceInvitations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Shadow FK. The relationship itself is configured in WorkspaceConfig.
        builder.Property<Guid>("WorkspaceId");

        builder.Property(x => x.InvitedUserId).IsRequired();
        builder.Property(x => x.InvitedBy).IsRequired();

        builder.Property(x => x.AssignedRole)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.AcceptedAtUtc);
        builder.Property(x => x.RevokedAtUtc);

        // "Show me my pending invitations" across all workspaces.
        builder.HasIndex(x => new { x.InvitedUserId, x.Status });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("WorkspaceId", nameof(WorkspaceInvitation.Status));
    }
}