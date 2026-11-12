namespace Application.Dto;

public record CommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
