using Domain.Aggregates.WorkspaceDocuments;
using Domain.Shared.ResultPattern;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.EntityConfiguration;
using Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace WorkspaceDocuments.Tests;

// Model/DDL generation and simulated provider failures only: no database is contacted.
public class WorkspaceDocumentPersistenceTests
{
    private static DbContextOptions<TaskPulseDbContext> Options =>
        new DbContextOptionsBuilder<TaskPulseDbContext>().UseNpgsql().Options;

    [Fact]
    public void Metadata_has_scoped_unique_index_case_sensitive_id_and_real_foreign_keys()
    {
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(WorkspaceDocument))!;
        Assert.Equal("WorkspaceDocuments", entity.GetTableName());
        Assert.Equal(150, entity.FindProperty(nameof(WorkspaceDocument.Title))!.GetMaxLength());
        Assert.Equal(256, entity.FindProperty(nameof(WorkspaceDocument.GoogleFileId))!.GetMaxLength());
        Assert.Equal("C", entity.FindProperty(nameof(WorkspaceDocument.GoogleFileId))!.GetCollation());
        Assert.True(entity.FindProperty(nameof(WorkspaceDocument.UpdatedAtUtc))!.IsConcurrencyToken);
        var index = Assert.Single(entity.GetIndexes(), x => x.GetDatabaseName() == WorkspaceDocumentConfig.UniqueFileIndex);
        Assert.True(index.IsUnique);
        Assert.Equal(new[] { "WorkspaceId", "GoogleFileId" }, index.Properties.Select(x => x.Name));
        Assert.Equal(3, entity.GetForeignKeys().Count());
        Assert.All(entity.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [Fact]
    public void Npgsql_generates_unique_and_foreign_key_constraints_without_opening_a_connection()
    {
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        var sql = context.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE \"WorkspaceDocuments\"", sql);
        Assert.Contains("CREATE UNIQUE INDEX \"" + WorkspaceDocumentConfig.UniqueFileIndex + "\"", sql);
        Assert.Contains("COLLATE \"C\"", sql);
        Assert.Contains("FOREIGN KEY (\"WorkspaceId\") REFERENCES \"Workspaces\"", sql);
    }

    [Fact]
    public void Generated_migration_only_adds_document_structure_and_snapshot_matches_the_model()
    {
        var migration = new AddWorkspaceDocuments();
        var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
        Assert.Equal("WorkspaceDocuments", table.Name);
        Assert.Equal(3, table.ForeignKeys.Count);
        Assert.All(migration.UpOperations, operation => Assert.True(operation is CreateTableOperation { Name: "WorkspaceDocuments" }
            or CreateIndexOperation { Table: "WorkspaceDocuments" }));
        Assert.IsType<DropTableOperation>(Assert.Single(migration.DownOperations));
        using var context = new TaskPulseDbContext(Options, TimeProvider.System);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Only_the_document_unique_constraint_maps_to_duplicate(bool documentConstraint)
    {
        var postgres = new PostgresException("synthetic duplicate", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
            constraintName: documentConstraint ? WorkspaceDocumentConfig.UniqueFileIndex : "UnrelatedIndex");
        await using var context = new FailedSaveContext(new DbUpdateException("synthetic failure", postgres));
        var result = await new UnitOfWork(context, NullLogger<UnitOfWork>.Instance)
            .SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.Equal(documentConstraint ? "WorkspaceDocument.Duplicate" : "Database.UpdateFailed", result.Error.code);
        Assert.Equal(documentConstraint ? ErrorType.Conflict : ErrorType.Problem, result.Error.errorType);
    }

    [Fact]
    public async Task Matching_constraint_with_non_unique_sqlstate_is_not_a_duplicate()
    {
        var postgres = new PostgresException("synthetic failure", "ERROR", "ERROR", PostgresErrorCodes.ForeignKeyViolation,
            constraintName: WorkspaceDocumentConfig.UniqueFileIndex);
        await using var context = new FailedSaveContext(new DbUpdateException("synthetic failure", postgres));
        var result = await new UnitOfWork(context, NullLogger<UnitOfWork>.Instance)
            .SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Database.UpdateFailed", result.Error.code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Only_document_entry_concurrency_failures_receive_the_document_conflict(bool documentEntry)
    {
        await using var tracking = new TaskPulseDbContext(Options, TimeProvider.System);
        object entity = documentEntry
            ? WorkspaceDocument.Create(Guid.NewGuid(), "Notes", "AbCdEfGhIjKl", WorkspaceDocumentSource.Manual,
                null, Guid.NewGuid(), DateTime.UtcNow).value
            : global::Domain.Aggregates.Collaboration.Workspaces.Workspace.Workspace.Create("Test", Guid.NewGuid()).value;
        var failure = new EntryConcurrencyException(tracking.Entry(entity));
        await using var context = new FailedSaveContext(failure);
        var result = await new UnitOfWork(context, NullLogger<UnitOfWork>.Instance)
            .SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(documentEntry ? "WorkspaceDocument.Changed" : "Database.UpdateFailed", result.Error.code);
        Assert.Equal(documentEntry ? ErrorType.Conflict : ErrorType.Problem, result.Error.errorType);
    }

    [Theory]
    [InlineData("AbCdEfGhIjKl", true)]
    [InlineData("AbCdEfGh_-12", true)]
    [InlineData("AbCdEfGhIjKl\n", false)]
    [InlineData("../AbCdEfGhIjKl", false)]
    [InlineData("https://docs.google.com/document/d/AbCdEfGhIjKl/edit", false)]
    public void Canonical_id_validation_matches_frontend_alphabet_without_accepting_urls(string id, bool valid) =>
        Assert.Equal(valid, WorkspaceDocument.IsValidGoogleFileId(id));

    [Fact]
    public void Domain_preserves_identifier_case_and_generates_record_identity_and_timestamps()
    {
        var actor = Guid.NewGuid();
        var now = DateTime.SpecifyKind(new DateTime(2026, 11, 21), DateTimeKind.Utc);
        var created = WorkspaceDocument.Create(Guid.NewGuid(), "  Notes  ", "AbCdEfGhIjKl",
            WorkspaceDocumentSource.Manual, null, actor, now).value;
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("AbCdEfGhIjKl", created.GoogleFileId);
        Assert.Equal("Notes", created.Title);
        Assert.Equal(actor, created.CreatedByUserId);
        Assert.Equal(now, created.CreatedAtUtc);
        Assert.Equal(now, created.UpdatedAtUtc);
        Assert.Null(created.UpdatedByUserId);
        Assert.True(created.Rename(" New title ", actor, now.AddSeconds(1)).IsSuccess);
        Assert.Equal("New title", created.Title);
        Assert.Equal(now.AddSeconds(1), created.UpdatedAtUtc);
        Assert.Equal(now, created.CreatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-10)]
    public void Rename_advances_the_token_at_database_precision_even_when_clock_does_not_advance(int clockTicks)
    {
        var actor = Guid.NewGuid();
        var now = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc);
        var document = WorkspaceDocument.Create(Guid.NewGuid(), "Notes", "AbCdEfGhIjKl",
            WorkspaceDocumentSource.Manual, null, actor, now).value;

        Assert.True(document.Rename("First rename", actor, now.AddTicks(clockTicks)).IsSuccess);
        Assert.True(document.UpdatedAtUtc.Ticks / 10 > now.Ticks / 10);
        var firstVersion = document.UpdatedAtUtc;
        Assert.True(document.Rename("Second rename", actor, now).IsSuccess);
        Assert.True(document.UpdatedAtUtc.Ticks / 10 > firstVersion.Ticks / 10);
        Assert.Equal(now, document.CreatedAtUtc);
    }

    [Fact]
    public void Creation_normalizes_audit_timestamps_to_database_precision()
    {
        var now = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc).AddTicks(19);
        var document = WorkspaceDocument.Create(Guid.NewGuid(), "Notes", "AbCdEfGhIjKl",
            WorkspaceDocumentSource.Manual, null, Guid.NewGuid(), now).value;
        Assert.Equal(now.AddTicks(-9), document.CreatedAtUtc);
        Assert.Equal(document.CreatedAtUtc, document.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, document.UpdatedAtUtc.Kind);
    }

    private sealed class EntryConcurrencyException(EntityEntry entry) : DbUpdateConcurrencyException("synthetic conflict")
    {
        public override IReadOnlyList<EntityEntry> Entries => [entry];
    }

    private sealed class FailedSaveContext(Exception failure) : TaskPulseDbContext(Options, TimeProvider.System)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromException<int>(failure);
    }
}
