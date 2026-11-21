using Domain.Aggregates.Tasks.TaskDocument;

namespace Application.Dto;

public record TaskDocumentDto(Guid Id, Guid TaskId, string Label, string Url, DateTime CreatedAtUtc,
    Guid CreatedByUserId, DateTime? UpdatedAtUtc)
{
    public static TaskDocumentDto From(TaskDocument document) => new(document.Id, document.TaskId,
        document.Label, document.Url, document.CreatedAtUtc, document.CreatedByUserId, document.UpdatedAtUtc);
}

public record TaskDocumentRequestDto(string Url, string Label);
