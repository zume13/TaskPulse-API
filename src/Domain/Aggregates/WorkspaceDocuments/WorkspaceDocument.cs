using System.Text.RegularExpressions;
using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.WorkspaceDocuments;

public enum WorkspaceDocumentSource
{
    Manual = 1,
    Uploaded = 2
}

/// <summary>TaskPulse metadata only. Google access and file contents are deliberately outside this aggregate.</summary>
public sealed class WorkspaceDocument : AggregateRoot
{
    public const int MaxTitleLength = 150;
    public const int MaxGoogleFileIdLength = 256;
    public const string DocxMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string GoogleDocsMimeType = "application/vnd.google-apps.document";
    private static readonly Regex FileIdPattern = new("\\A[A-Za-z0-9_-]{10,256}\\z", RegexOptions.CultureInvariant);

    private WorkspaceDocument() { }

    public Guid WorkspaceId { get; private set; }
    public string Title { get; private set; } = null!;
    public string GoogleFileId { get; private set; } = null!;
    public WorkspaceDocumentSource SourceType { get; private set; }
    public string? MimeType { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static bool IsValidTitle(string? title) =>
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= MaxTitleLength;

    // Same alphabet/minimum as the frontend; a bounded size also limits persisted untrusted input.
    public static bool IsValidGoogleFileId(string? id) => id is not null && FileIdPattern.IsMatch(id);

    public static bool IsValidSource(WorkspaceDocumentSource source, string? mimeType) => source switch
    {
        WorkspaceDocumentSource.Manual => mimeType is null || mimeType == GoogleDocsMimeType,
        WorkspaceDocumentSource.Uploaded => mimeType == DocxMimeType,
        _ => false
    };

    public static ResultT<WorkspaceDocument> Create(Guid workspaceId, string? title, string? googleFileId,
        WorkspaceDocumentSource sourceType, string? mimeType, Guid actorId, DateTime nowUtc)
    {
        if (workspaceId == Guid.Empty || actorId == Guid.Empty || !IsValidTitle(title)
            || !IsValidGoogleFileId(googleFileId) || !IsValidSource(sourceType, mimeType))
            return WorkspaceDocumentErrors.InvalidMetadata;

        nowUtc = AtDatabasePrecision(nowUtc);
        return new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = title!.Trim(),
            GoogleFileId = googleFileId!,
            SourceType = sourceType,
            MimeType = mimeType,
            CreatedByUserId = actorId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public Result Rename(string? title, Guid actorId, DateTime nowUtc)
    {
        if (!IsValidTitle(title) || actorId == Guid.Empty)
            return WorkspaceDocumentErrors.InvalidMetadata;

        Title = title!.Trim();
        // PostgreSQL timestamps store microseconds, not DateTime's 100ns ticks.
        // A repeated/backward clock must not leave an update with the old concurrency token.
        var clockTime = AtDatabasePrecision(nowUtc);
        UpdatedAtUtc = clockTime > UpdatedAtUtc ? clockTime : AtDatabasePrecision(UpdatedAtUtc).AddTicks(10);
        UpdatedByUserId = actorId;
        return Result.Success();
    }

    private static DateTime AtDatabasePrecision(DateTime value) => new(value.Ticks - value.Ticks % 10, value.Kind);
}
