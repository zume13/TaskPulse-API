using Domain.Aggregates.Tasks.TaskComment.ValueObjects;
using LMS.SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.TaskComment
{
    public class Comment : Entity
    {
        private Comment(Guid id, Guid authorId, CommentBody body) : base(id)
        {
            AuthorId = authorId;
            Body = body;
            CreatedAtUtc = DateTime.UtcNow; 
        }
        private Comment() { }
        public Guid AuthorId { get; private set; }
        public CommentBody Body { get; private set; } = null!;
        public DateTime? CreatedAtUtc { get; private set; }
        
        public static ResultT<Comment> Create(Guid authorId, CommentBody body)
        {
            return new Comment(Guid.NewGuid(), authorId, body);
        }
    }
}
    