using Domain.Aggregates.Tasks.Task.Errors;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Aggregates.Tasks.TaskDocument;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Tasks;

public partial class TaskItem
{
    private readonly List<TaskDocument.TaskDocument> _documents = [];
    public IReadOnlyCollection<TaskDocument.TaskDocument> Documents => _documents.AsReadOnly();

    public ResultT<TaskDocument.TaskDocument> AddDocument(DocumentLink link, Guid actorId, DateTime nowUtc)
    {
        if (IsDeleted) return TaskErrors.TaskDeleted;
        if (link is null) return GeneralErrors.General.Empty(nameof(link));
        if (actorId == Guid.Empty) return GeneralErrors.General.Empty(nameof(actorId));
        if (HasDuplicateUrl(link.Url)) return TaskDocumentCollectionErrors.Duplicate;
        var document = TaskDocument.TaskDocument.Create(Id, link, actorId, nowUtc);
        _documents.Add(document);
        return document;
    }

    public Result UpdateDocument(Guid documentId, DocumentLink link, DateTime nowUtc)
    {
        if (IsDeleted) return TaskErrors.TaskDeleted;
        if (link is null) return GeneralErrors.General.Empty(nameof(link));
        var document = _documents.FirstOrDefault(x => x.Id == documentId);
        if (document is null) return TaskDocumentCollectionErrors.NotFound;
        if (HasDuplicateUrl(link.Url, documentId)) return TaskDocumentCollectionErrors.Duplicate;
        document.Update(link, nowUtc);
        if (document.IsLegacy) LinkedDocument = link;
        return Result.Success();
    }

    public Result RemoveDocument(Guid documentId)
    {
        if (IsDeleted) return TaskErrors.TaskDeleted;
        var document = _documents.FirstOrDefault(x => x.Id == documentId);
        if (document is null) return TaskDocumentCollectionErrors.NotFound;
        _documents.Remove(document);
        if (document.IsLegacy) LinkedDocument = null;
        return Result.Success();
    }

    public Result UpdateDocumentLink(DocumentLink link, Guid actorId, DateTime nowUtc)
    {
        if (IsDeleted) return TaskErrors.TaskDeleted;
        if (link is null) return GeneralErrors.General.Empty(nameof(link));
        if (actorId == Guid.Empty) return GeneralErrors.General.Empty(nameof(actorId));
        var legacy = _documents.SingleOrDefault(x => x.IsLegacy);
        if (HasDuplicateUrl(link.Url, legacy?.Id)) return TaskDocumentCollectionErrors.Duplicate;
        if (legacy is null) _documents.Add(TaskDocument.TaskDocument.Create(Id, link, actorId, nowUtc, legacy: true));
        else legacy.Update(link, nowUtc);
        LinkedDocument = link;
        return Result.Success();
    }

    private bool HasDuplicateUrl(string url, Guid? exceptId = null) =>
        _documents.Any(x => x.Id != exceptId && x.UrlHash == TaskDocument.TaskDocument.HashUrl(url));
}
