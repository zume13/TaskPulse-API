using Domain.Aggregates.Tasks.TaskComment;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class CommentConfig : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("TaskComments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Shadow FK. The relationship itself is configured in TaskItemConfig.
        builder.Property<Guid>("TaskItemId");

        builder.Property(x => x.AuthorId).IsRequired();

        builder.OwnsOne(x => x.Body, body =>
        {
            body.Property(p => p.Value)
                .HasColumnName("Body")
                .IsRequired()
                .HasMaxLength(500);
        });
        builder.Navigation(x => x.Body).IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);

        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Loads a task's comments in chronological order.
        builder.HasIndex("TaskItemId", nameof(Comment.CreatedAtUtc));
    }
}