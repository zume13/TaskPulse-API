using Domain.Aggregates.Tasks.Task.Enums;
using Domain.Aggregates.Tasks.Task.Errors;
using Domain.Aggregates.Tasks.TaskComment;
using Domain.Aggregates.Tasks.TaskComment.ValueObjects;
using Domain.Shared.ValueObjects;
using SharedKernel.Primitives;
using Shared.ResultPattern;

namespace Domain.Aggregates.Tasks.Task
{
    public class Task : AggregateRoot
    {
        private readonly List<Comment> _comments = [];

        public Guid WorkspaceId { get; private set; }
        public Name Title { get; private set; } = null!;
        public DateTime? DueDate { get; private set; }
        public Status Status { get; private set; }        
        public Guid? AssignedTo { get; private set; }
        public Guid? AssignBy { get; private set; }
        public Guid? MarkedCompletedBy { get; private set; }
        public Guid CreatedBy { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

        private Task() { }   // EF Core

        private Task(Guid id, Guid workspaceId, Name title, DateTime? dueDate, Guid createdBy) : base(id)
        {
            WorkspaceId = workspaceId;
            Title = title;
            DueDate = dueDate;
            Status = Status.Pending;
            CreatedBy = createdBy;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public static ResultT<Task> Create(Guid workspaceId, Name title, DateTime? dueDate, Guid createdBy)
        {
            return new Task(Guid.NewGuid(), workspaceId, title, dueDate, createdBy);
        }

        /// <remarks>
        /// Trusts that the caller (Application layer) has verified assignee and assigner are members of
        /// this task's workspace. The aggregate cannot check that: it would need to load Workspace.
        /// </remarks>
        public Result AssignTo(Guid assigneeId, Guid assignedBy, DateTime nowUtc)
        {
            if (Status == Status.Completed)
                return TaskErrors.InvalidStatus;

            if (AssignedTo == assigneeId)
                return TaskErrors.InvalidAssignee;

            AssignBy = assignedBy;
            AssignedTo = assigneeId;

            return Result.Success();
        }

        public Result Unassign()
        {
            if (Status == Status.Completed)
                return TaskErrors.InvalidStatus;

            if (AssignedTo is null)
                return TaskErrors.NullAssignee;

            AssignedTo = null;

            return Result.Success();
        }

        public Result Complete(Guid completedBy)
        {
            if (Status == Status.Completed)
                return TaskErrors.InvalidStatus;

            Status = Status.Completed;
            CompletedAtUtc = DateTime.UtcNow;
            MarkedCompletedBy = completedBy;

            return Result.Success();
        }

        /// <remarks>Trusts the caller to have verified the author is a workspace member.</remarks>
        public void AddComment(Guid authorId, CommentBody body, DateTime nowUtc)
        {
            var comment = Comment.Create(authorId, body);

            _comments.Add(comment.value);
        }

        /// <summary>Overdue is a pure function of stored status, due date and the clock. Never persisted.</summary>
        public Status GetEffectiveStatus(DateTime nowUtc) =>
            Status == Status.Pending && DueDate is not null && DueDate > DateTime.UtcNow ? Status : Status.Overdue;

        public bool IsOverdue(DateTime nowUtc) => GetEffectiveStatus(nowUtc) == Status.Overdue;
    }
}
