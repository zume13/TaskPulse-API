using Domain.Aggregates.Collaboration.Workspaces.Workspace;
using Domain.Aggregates.User;
using Domain.Aggregates.WorkspaceDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public sealed class WorkspaceDocumentConfig : IEntityTypeConfiguration<WorkspaceDocument>
{
    public const string UniqueFileIndex = "IX_WorkspaceDocuments_WorkspaceId_GoogleFileId";

    public void Configure(EntityTypeBuilder<WorkspaceDocument> builder)
    {
        builder.ToTable("WorkspaceDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Title).HasMaxLength(WorkspaceDocument.MaxTitleLength).IsRequired();
        builder.Property(x => x.GoogleFileId).HasMaxLength(WorkspaceDocument.MaxGoogleFileIdLength)
            .UseCollation("C").IsRequired();
        builder.Property(x => x.SourceType).HasConversion<int>().IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(150);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired().IsConcurrencyToken();
        builder.HasIndex(x => new { x.WorkspaceId, x.GoogleFileId }).IsUnique().HasDatabaseName(UniqueFileIndex);
        builder.HasOne<Workspace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
