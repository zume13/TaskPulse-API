using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class WorkspaceConfig : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Ignore(x => x.DomainEvents);

        builder.OwnsOne(x => x.WorkspaceName, name =>
        {
            name.Property(p => p.Value)
                .HasColumnName("Name")
                .IsRequired()
                .HasMaxLength(100);
        });
        builder.Navigation(x => x.WorkspaceName).IsRequired();

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.IsArchived).IsRequired();
        builder.Property(x => x.ArchivedAtUtc);

        // Restrict: deleting a user must never silently delete their workspaces.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Members and invitations belong to the aggregate: they are loaded,
        // changed and deleted through the Workspace, never on their own.
        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey("WorkspaceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Invitations)
            .WithOne()
            .HasForeignKey("WorkspaceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Invitations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}