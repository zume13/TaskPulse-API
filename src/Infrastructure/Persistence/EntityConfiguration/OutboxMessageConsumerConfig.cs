using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class OutboxMessageConsumerConfig : IEntityTypeConfiguration<OutboxMessageConsumer>
{
    public void Configure(EntityTypeBuilder<OutboxMessageConsumer> builder)
    {
        builder.ToTable("OutboxMessageConsumers");

        // One row per (message, handler). The key is also the idempotency check.
        builder.HasKey(x => new { x.OutboxMessageId, x.Handler });

        builder.Property(x => x.Handler)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.ConsumedAtUtc).IsRequired();

        // Deleting old outbox messages also deletes their consumer rows.
        builder.HasOne<OutboxMessage>()
            .WithMany()
            .HasForeignKey(x => x.OutboxMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
