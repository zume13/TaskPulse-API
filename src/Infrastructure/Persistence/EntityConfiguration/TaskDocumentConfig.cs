using Domain.Aggregates.Tasks.TaskDocument;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public sealed class TaskDocumentConfig : IEntityTypeConfiguration<TaskDocument>
{
    public const string UniqueUrlIndex = "IX_TaskDocuments_TaskId_UrlHash";
    public const string LegacySlotIndex = "IX_TaskDocuments_TaskId_LegacySlot";
    public void Configure(EntityTypeBuilder<TaskDocument> builder)
    {
        builder.ToTable("TaskDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Url).IsRequired(); // text: never truncate preexisting legacy URLs.
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UrlHash).HasMaxLength(64).UseCollation("C").IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TaskId, x.UrlHash }).IsUnique().HasDatabaseName(UniqueUrlIndex);
        builder.HasIndex(x => x.TaskId).IsUnique().HasFilter("\"IsLegacy\"").HasDatabaseName(LegacySlotIndex);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
