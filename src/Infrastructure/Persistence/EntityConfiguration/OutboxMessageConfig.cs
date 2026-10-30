using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class OutboxMessageConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.NextAttemptAtUtc).IsRequired();
        builder.Property(x => x.Attempts).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.LockedUntilUtc);
        builder.Property(x => x.ProcessedAtUtc);
        builder.Property(x => x.FailedAtUtc);

        // The job only looks at rows that are neither processed nor
        // dead-lettered, so index just those. The index stays tiny.
        builder.HasIndex(x => x.NextAttemptAtUtc)
            .HasDatabaseName("IX_OutboxMessages_Pending")
            .HasFilter("\"ProcessedAtUtc\" IS NULL AND \"FailedAtUtc\" IS NULL");

        // For the cleanup job that deletes old processed rows.
        builder.HasIndex(x => x.ProcessedAtUtc);
    }
}
