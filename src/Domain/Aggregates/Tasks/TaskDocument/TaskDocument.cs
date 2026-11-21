using System.Security.Cryptography;
using System.Text;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Shared.Primitives;

namespace Domain.Aggregates.Tasks.TaskDocument;

/// <summary>Child entity owned and mutated exclusively through TaskItem.</summary>
public sealed class TaskDocument : Entity
{
    private TaskDocument() { }
    public Guid TaskId { get; private set; }
    public string Url { get; private set; } = null!;
    public string Label { get; private set; } = null!;
    public string UrlHash { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid Version { get; private set; }
    public bool IsLegacy { get; private set; }

    internal static TaskDocument Create(Guid taskId, DocumentLink link, Guid actorId, DateTime nowUtc, bool legacy = false) => new()
    {
        Id = Guid.NewGuid(), TaskId = taskId, Url = link.Url, Label = link.Label,
        UrlHash = HashUrl(link.Url), CreatedByUserId = actorId, CreatedAtUtc = nowUtc,
        Version = Guid.NewGuid(), IsLegacy = legacy
    };

    internal void Update(DocumentLink link, DateTime nowUtc)
    {
        Url = link.Url; Label = link.Label; UrlHash = HashUrl(link.Url);
        UpdatedAtUtc = nowUtc; Version = Guid.NewGuid();
    }

    public static string HashUrl(string url) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
}
