using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Aggregates.Tasks.TaskDocument;
using Domain.Shared.ValueObjects;
using Domain.Shared.ResultPattern;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.EntityConfiguration;
using Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace TaskDocuments.Tests;

// Offline provider model/SQL inspection and simulated errors, NOT an applied PostgreSQL migration test.
public class TaskDocumentPersistenceTests
{
    private static DbContextOptions<TaskPulseDbContext> Options => new DbContextOptionsBuilder<TaskPulseDbContext>().UseNpgsql().Options;

    [Theory]
    [InlineData("https://docs.google.com/document/d/AbCdEfGhIjKl/edit")]
    [InlineData("https://docs.google.com/spreadsheets/d/AbCdEfGhIjKl/edit#gid=12")]
    [InlineData("https://docs.google.com/presentation/d/AbCdEfGhIjKl/view")]
    [InlineData("https://word.cloud.microsoft/open/AbCdEfGhIjKl")]
    [InlineData("https://excel.cloud.microsoft/open/AbCdEfGhIjKl")]
    [InlineData("https://powerpoint.cloud.microsoft/open/AbCdEfGhIjKl")]
    [InlineData("https://word.office.com/open/AbCdEfGhIjKl")]
    [InlineData("https://excel.office.com/open/AbCdEfGhIjKl")]
    [InlineData("https://powerpoint.office.com/open/AbCdEfGhIjKl")]
    [InlineData("https://office.com/open/AbCdEfGhIjKl")]
    [InlineData("https://microsoft365.com/open/AbCdEfGhIjKl")]
    [InlineData("https://1drv.ms/w/AbCdEfGhIjKl")]
    [InlineData("https://github.com/example/project/blob/main/Readme.md")]
    [InlineData("https://drive.google.com/file/d/AbCdEfGhIjKl/view")]
    public void Supported_existing_providers_and_uploaded_drive_files_keep_url_correctness(string url)
    {
        var link = DocumentLink.Create(url, "  Notes  ");
        Assert.True(link.IsSuccess); Assert.Equal(url, link.value.Url); Assert.Equal("Notes", link.value.Label);
    }

    [Theory]
    [InlineData("document")]
    [InlineData("spreadsheets")]
    [InlineData("presentation")]
    public void Google_browser_session_is_not_persisted_but_case_and_document_parameters_are_preserved(string editor)
    {
        var link = DocumentLink.Create($"https://docs.google.com/{editor}/u/3/d/AbCdEfGhIjKl/edit?authuser=3&tab=t.1#heading=h.test", "Notes").value;
        Assert.Equal($"https://docs.google.com/{editor}/d/AbCdEfGhIjKl/edit?tab=t.1#heading=h.test", link.Url);
        Assert.DoesNotContain("authuser", link.Url); Assert.DoesNotContain("/u/3/", link.Url);
    }

    [Theory]
    [InlineData("https://drive.google.com/folders/AbCdEfGhIjKl")]
    [InlineData("https://drive.google.com.evil.example/file/d/AbCdEfGhIjKl/view")]
    [InlineData("https://github.com/settings/profile")]
    [InlineData("https://github.com/example/project/unknown/part")]
    public void Unsupported_paths_are_not_widened(string url) => Assert.True(DocumentLink.Create(url, "Notes").IsFailure);

    [Theory]
    [InlineData("#access_token=synthetic")]
    [InlineData("#heading=h.test&id_token=synthetic")]
    [InlineData("?%72efresh_token=synthetic")]
    [InlineData("#ACCESS_TOKEN=synthetic")]
    public void Google_credentials_in_query_or_fragment_are_rejected(string suffix) =>
        Assert.True(DocumentLink.Create(TestApi.Url + suffix, "Notes").IsFailure);

    [Fact]
    public void Google_session_selectors_are_removed_from_query_and_fragment()
    {
        var link = DocumentLink.Create(TestApi.Url + "?%61uthuser=1&tab=t.1#authuser=2&heading=h.test", "Notes").value;
        Assert.Equal(TestApi.Url + "?tab=t.1#heading=h.test", link.Url);
    }

    [Fact]
    public void Stable_child_identity_duplicate_detection_and_legacy_mirroring_are_task_owned()
    {
        var creator = Guid.NewGuid(); var task = TestApi.MakeTask(Guid.NewGuid(), creator);
        var link = DocumentLink.Create(TestApi.Url, "One").value;
        var added = task.AddDocument(link, creator, TestApi.Now).value;
        var id = added.Id; var version = added.Version;
        Assert.Equal(task.Id, added.TaskId); Assert.Equal(creator, added.CreatedByUserId);
        Assert.Equal("TaskDocument.Duplicate", task.AddDocument(link, creator, TestApi.Now).Error.code);
        var next = DocumentLink.Create(TestApi.OtherUrl, "Two").value;
        Assert.True(task.UpdateDocument(id, next, TestApi.Now).IsSuccess);
        Assert.Equal(id, added.Id); Assert.NotEqual(version, added.Version);
        Assert.Null(task.LinkedDocument);
        Assert.True(task.UpdateDocumentLink(link, creator, TestApi.Now).IsSuccess);
        var legacy = Assert.Single(task.Documents, x => x.IsLegacy);
        Assert.True(task.UpdateDocument(legacy.Id, DocumentLink.Create(TestApi.Url, "Legacy changed").value, TestApi.Now).IsSuccess);
        Assert.Equal("Legacy changed", task.LinkedDocument!.Label);
        Assert.True(task.RemoveDocument(legacy.Id).IsSuccess); Assert.Null(task.LinkedDocument);
        Assert.Equal(id, Assert.Single(task.Documents).Id);
    }

    [Fact]
    public void Model_preserves_old_columns_and_adds_task_owned_collection_with_database_guards()
    {
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(TaskDocument))!;
        Assert.Equal("TaskDocuments", entity.GetTableName());
        Assert.True(entity.FindProperty("Version")!.IsConcurrencyToken);
        Assert.Equal("C", entity.FindProperty("UrlHash")!.GetCollation());
        Assert.Equal(64, entity.FindProperty("UrlHash")!.GetMaxLength());
        var index = Assert.Single(entity.GetIndexes(), x => x.GetDatabaseName() == TaskDocumentConfig.UniqueUrlIndex);
        Assert.True(index.IsUnique); Assert.Equal(new[] { "TaskId", "UrlHash" }, index.Properties.Select(x => x.Name));
        Assert.Equal("\"IsLegacy\"", Assert.Single(entity.GetIndexes(), x => x.GetDatabaseName() == TaskDocumentConfig.LegacySlotIndex).GetFilter());
        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(entity.GetForeignKeys(), x => x.Properties[0].Name == "TaskId").DeleteBehavior);
        var sql = context.Database.GenerateCreateScript();
        Assert.Contains("\"LinkedDocumentUrl\"", sql); Assert.Contains("\"LinkedDocumentLabel\"", sql);
        Assert.DoesNotContain("WorkspaceDocuments", sql);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Migration_is_additive_backfills_every_nonnull_legacy_url_and_guards_downgrades()
    {
        var migration = new AddTaskDocuments();
        var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
        Assert.Equal("TaskDocuments", table.Name);
        Assert.Equal(2, table.ForeignKeys.Count);
        Assert.DoesNotContain(migration.UpOperations, x => x is DropColumnOperation or DropTableOperation or AlterColumnOperation);
        var backfill = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;
        Assert.Contains("INSERT INTO \"TaskDocuments\"", backfill);
        Assert.Contains("SELECT \"Id\", \"Id\", \"LinkedDocumentUrl\"", backfill);
        Assert.Contains("WHERE \"LinkedDocumentUrl\" IS NOT NULL", backfill);
        Assert.DoesNotContain("IsDeleted", backfill); // Soft-deleted task data is preserved too.
        Assert.Contains("sha256(convert_to(\"LinkedDocumentUrl\", 'UTF8'))", backfill);
        Assert.Contains("\"CreatedBy\", \"CreatedAtUtc\", NULL, \"Id\", TRUE", backfill);
        Assert.DoesNotContain("UPDATE \"TaskItems\"", backfill); // Original columns are not rewritten.
        var guard = Assert.IsType<SqlOperation>(migration.DownOperations[0]);
        Assert.Contains("RAISE EXCEPTION", guard.Sql); Assert.Contains("NOT \"IsLegacy\"", guard.Sql);
        Assert.Contains("d.\"Version\" <> t.\"Id\"", guard.Sql);
        Assert.Contains("d.\"Label\" IS DISTINCT FROM COALESCE(t.\"LinkedDocumentLabel\", 'Legacy document')", guard.Sql);
        Assert.IsType<DropTableOperation>(migration.DownOperations[1]);
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        var script = context.GetService<IMigrator>().GenerateScript("20261116142659_AddLoginAudits", "20261121105737_AddTaskDocuments");
        Assert.Contains("INSERT INTO \"TaskDocuments\"", script);
        Assert.DoesNotContain("DROP TABLE", script); Assert.DoesNotContain("CREATE TABLE \"TaskItems\"", script);
        var rollback = context.GetService<IMigrator>().GenerateScript("20261121105737_AddTaskDocuments", "20261116142659_AddLoginAudits");
        Assert.Contains("COALESCE(t.\"LinkedDocumentLabel\", 'Legacy document')", rollback);
        Assert.True(rollback.IndexOf("RAISE EXCEPTION", StringComparison.Ordinal) < rollback.IndexOf("DROP TABLE", StringComparison.Ordinal));
    }

    [Fact]
    public void Ef_tracks_child_update_versions_and_collection_orphan_deletes_without_a_connection()
    {
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        var task = TestApi.MakeTask(Guid.NewGuid(), Guid.NewGuid());
        var document = task.AddDocument(DocumentLink.Create(TestApi.Url, "One").value, task.CreatedBy, TestApi.Now).value;
        context.Attach(task);
        var before = document.Version;
        task.UpdateDocument(document.Id, DocumentLink.Create(TestApi.OtherUrl, "Updated").value, TestApi.Now);
        context.ChangeTracker.DetectChanges();
        Assert.Equal(before, context.Entry(document).Property(x => x.Version).OriginalValue);
        Assert.NotEqual(before, context.Entry(document).Property(x => x.Version).CurrentValue);
        task.RemoveDocument(document.Id); context.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Deleted, context.Entry(document).State);
    }

    [Theory]
    [InlineData(TaskDocumentConfig.UniqueUrlIndex, PostgresErrorCodes.UniqueViolation, "TaskDocument.Duplicate")]
    [InlineData(TaskDocumentConfig.LegacySlotIndex, PostgresErrorCodes.UniqueViolation, "TaskDocument.Changed")]
    [InlineData("UnrelatedIndex", PostgresErrorCodes.UniqueViolation, "Database.UpdateFailed")]
    [InlineData(TaskDocumentConfig.UniqueUrlIndex, PostgresErrorCodes.ForeignKeyViolation, "Database.UpdateFailed")]
    public async Task Provider_errors_are_mapped_only_for_the_exact_new_constraints(string constraint, string sqlState, string expected)
    {
        var postgres = new PostgresException("Synthetic error", "ERROR", "ERROR", sqlState, constraintName: constraint);
        await using var context = new FailingContext(new DbUpdateException("Synthetic error", postgres));
        var result = await new UnitOfWork(context, NullLogger<UnitOfWork>.Instance).SaveChangesAsync(TestApi.Ct);
        Assert.Equal(expected, result.Error.code);
    }

    [Fact]
    public async Task Only_document_entry_concurrency_conflicts_receive_the_document_error()
    {
        using var tracking = new TaskPulseDbContext(Options, TimeProvider.System);
        var task = TestApi.MakeTask(Guid.NewGuid(), Guid.NewGuid());
        var document = task.AddDocument(DocumentLink.Create(TestApi.Url, "One").value, task.CreatedBy, TestApi.Now).value;
        await using var context = new FailingContext(new EntryConflict(tracking.Entry(document)));
        var result = await new UnitOfWork(context, NullLogger<UnitOfWork>.Instance).SaveChangesAsync(TestApi.Ct);
        Assert.Equal("TaskDocument.Changed", result.Error.code); Assert.Equal(ErrorType.Conflict, result.Error.errorType);
    }

    [Fact]
    public void Attaching_a_new_document_to_an_already_tracked_task_is_an_insert_not_an_update()
    {
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        var task = TestApi.MakeTask(Guid.NewGuid(), Guid.NewGuid()); context.Attach(task);
        var document = task.AddDocument(DocumentLink.Create(TestApi.Url, "One").value, task.CreatedBy, TestApi.Now).value;
        context.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Added, context.Entry(document).State);
        Assert.Equal(EntityState.Unchanged, context.Entry(task).State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ef_task_detail_edits_do_not_modify_loaded_or_unloaded_document_relationships(bool loaded)
    {
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        var task = TestApi.MakeTask(Guid.NewGuid(), Guid.NewGuid());
        if (loaded)
        {
            task.UpdateDocumentLink(DocumentLink.Create(TestApi.Url, "Legacy").value, task.CreatedBy, TestApi.Now);
            task.AddDocument(DocumentLink.Create(TestApi.OtherUrl, "Extra").value, task.CreatedBy, TestApi.Now);
        }
        context.Attach(task);
        context.Entry(task).Collection(x => x.Documents).IsLoaded = loaded;
        task.UpdateDetails(Name.Create("Edited").value, DateTime.UtcNow.AddDays(10), TestApi.Now);
        task.UpdateDescription("Changed");
        context.ChangeTracker.DetectChanges();
        Assert.Equal(loaded, context.Entry(task).Collection(x => x.Documents).IsLoaded);
        Assert.All(context.ChangeTracker.Entries<TaskDocument>(), x => Assert.Equal(EntityState.Unchanged, x.State));
        Assert.All(context.ChangeTracker.Entries<DocumentLink>(), x => Assert.Equal(EntityState.Unchanged, x.State));
        Assert.Equal(loaded ? 2 : 0, task.Documents.Count);
    }

    [Fact]
    public void Escaped_non_google_urls_do_not_gain_a_new_storage_length_restriction()
    {
        var raw = "https://office.com/" + new string('é', 500);
        var link = DocumentLink.Create(raw, "Unicode path");
        Assert.True(link.IsSuccess);
        Assert.True(link.value.Url.Length > DocumentLink.MaxUrlLength);
        Assert.Equal(new Uri(raw).AbsoluteUri, link.value.Url);
    }

    private sealed class EntryConflict(EntityEntry entry) : DbUpdateConcurrencyException("Synthetic conflict")
    { public override IReadOnlyList<EntityEntry> Entries => [entry]; }
    private sealed class FailingContext(Exception failure) : TaskPulseDbContext(Options, TimeProvider.System)
    { public override Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromException<int>(failure); }
}
