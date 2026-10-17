using Domain.Aggregates.Tasks.TaskComment.ValueObjects;
using LMS.SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.TaskComment;

public class Comment : Entity
{
    private Comment(
        Guid id,
        Guid authorId,
        CommentBody body,
        DateTime nowUtc)
        : base(id)
    {
        AuthorId = authorId;
        Body = body;
        CreatedAtUtc = nowUtc;
    }

    private Comment() { }

    public Guid AuthorId { get; private set; }

    public CommentBody Body { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public static ResultT<Comment> Create(
        Guid authorId,
        CommentBody body,
        DateTime nowUtc)
    {
        if (authorId == Guid.Empty)
            return GeneralErrors.General.Empty(nameof(authorId));

        if (body is null)
            return GeneralErrors.General.Empty(nameof(body));

        return new Comment(
            Guid.NewGuid(),
            authorId,
            body,
            nowUtc);
    }

    public static ResultT<Comment> Create(
        Guid authorId,
        CommentBody body)
    {
        return Create(authorId, body, DateTime.UtcNow);
    }
}
