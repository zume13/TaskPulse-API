using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.Tasks;
using Domain.Aggregates.Tasks.Task.Enums;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class TaskItemConfig : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        // Overdue is computed from DueDate and the clock (GetEffectiveStatus).
        // The database refuses to store it.
        builder.ToTable("TaskItems", table => table.HasCheckConstraint(
            "CK_TaskItems_Status_NotOverdue",
            $"\"Status\" <> {(int)Status.Overdue}"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.WorkspaceId).IsRequired();

        builder.OwnsOne(x => x.Title, title =>
        {
            title.Property(p => p.Value)
                .HasColumnName("Title")
                .IsRequired()
                .HasMaxLength(100);
        });
        builder.Navigation(x => x.Title).IsRequired();

        builder.Property(x => x.Description);
        builder.Property(x => x.DueDate).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TaskPriority)
            .HasColumnName("Priority")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.AssignedTo);
        builder.Property(x => x.AssignBy);
        builder.Property(x => x.MarkedCompletedBy);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CompletedAtUtc);

        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAtUtc);
        builder.Property(x => x.DeletedBy);

        // Optional value object stored in the same table. Both columns are
        // NULL when the task has no linked document.
        builder.OwnsOne(x => x.LinkedDocument, document =>
        {
            document.Property(p => p.Url)
                .HasColumnName("LinkedDocumentUrl")
                .IsRequired();

            document.Property(p => p.Label)
                .HasColumnName("LinkedDocumentLabel")
                .IsRequired()
                .HasMaxLength(DocumentLink.MaxLabelLength);
        });

        // Foreign keys are only added for the columns the app queries by.
        // AssignBy, MarkedCompletedBy and DeletedBy are audit columns and
        // stay plain Guids so they do not each add an index.
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AssignedTo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Comments)
            .WithOne()
            .HasForeignKey("TaskItemId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Workspace task lists and dashboards filter by status.
        builder.HasIndex(x => new { x.WorkspaceId, x.Status });
    }
}