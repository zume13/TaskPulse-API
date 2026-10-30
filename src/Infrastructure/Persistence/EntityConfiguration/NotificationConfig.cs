using Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.RecipientId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(Notification.MaxTitleLength)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(Notification.MaxMessageLength)
            .IsRequired();

        builder.Property(x => x.WorkspaceId)
            .IsRequired(false);

        builder.Property(x => x.TargetType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TargetId)
            .IsRequired(false);

        builder.Property(x => x.IsRead)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ReadAtUtc)
            .IsRequired(false);

        // Efficiently retrieve a user's notifications.
        builder.HasIndex(x => new
        {
            x.RecipientId,
            x.IsRead,
            x.CreatedAtUtc
        })
        .IsDescending(false, false, true);

        // Efficiently retrieve notifications belonging to a workspace.
        builder.HasIndex(x => new
        {
            x.WorkspaceId,
            x.CreatedAtUtc
        })
        .IsDescending(false, true);
    }
}