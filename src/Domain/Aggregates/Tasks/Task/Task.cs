using Domain.Aggregates.Tasks.Task.Enums;
using Domain.Aggregates.Tasks.Task.Errors;
using Domain.Aggregates.Tasks.Task.ValueObjects;
using Domain.Aggregates.Tasks.TaskComment;
using Domain.Aggregates.Tasks.TaskComment.ValueObjects;
using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;
using Domain.Shared.ValueObjects;
using SharedKernel.Constants;


namespace Domain.Aggregates.Tasks.Task
{
    public class Task : AggregateRoot
    {
        private readonly List<Comment> _comments = [];

        public Guid WorkspaceId { get; private set; }
        public Name Title { get; private set; } = null!;
        public DateTime DueDate { get; private set; }
        public string? Description { get; private set; }
        public Status Status { get; private set; }        
        public Priority TaskPriority { get; private set; }

        public Guid? AssignedTo { get; private set; }
        public Guid? AssignBy { get; private set; }

        public Guid? MarkedCompletedBy { get; private set; }
        public Guid CreatedBy { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAtUtc { get; private set; }
        public Guid? DeletedBy { get; private set; }

        public DocumentLink? LinkedDocument { get; private set; }
        public IReadOnlyCollection<Comment> Comments => 
            _comments.AsReadOnly();

        private Task() { }   // EF Core

        private Task(Guid id, Guid workspaceId, Name title, DateTime dueDate, Guid createdBy, Priority priority, DateTime UtcNow, string? description = null) : base(id)
        {
            WorkspaceId = workspaceId;
            Title = title;
            DueDate = dueDate;
            Status = Status.Pending;
            TaskPriority = priority;
            Description = NormalizeDescription(description);
            CreatedBy = createdBy;
            CreatedAtUtc = UtcNow;
        }

        public static ResultT<Task> Create(Guid workspaceId, Name title, DateTime dueDate, Guid createdBy, Priority priority = Priority.Medium, string? description = null)
        {
            var utcNow = SystemConstants.UtcNow;

            if (workspaceId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(workspaceId));

            if (title is null)
                return GeneralErrors.General.Empty(nameof(title));

            if (dueDate <= utcNow)
                return TaskErrors.InvalidDueDate;

            if (createdBy == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(createdBy));

            if (!Enum.IsDefined(priority))
                return TaskErrors.InvalidPriority;

            return new Task(Guid.NewGuid(), workspaceId, title, dueDate, createdBy, priority, utcNow, description);
        }

        /// <remarks>
        /// The application layer must verify that the assignee and
        /// assigner are members of this task's workspace.
        /// </remarks>
        public Result AssignTo(
            Guid assigneeId,
            Guid assignedBy)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (Status == Status.Completed)
                return TaskErrors.InvalidStatus;

            if (assigneeId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(assigneeId));

            if (assignedBy == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(assignedBy));

            if (AssignedTo == assigneeId)
                return TaskErrors.InvalidAssignee;

            AssignedTo = assigneeId;
            AssignBy = assignedBy;

            return Result.Success();
        }

        public Result Unassign()
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (Status == Status.Completed)
                return TaskErrors.InvalidStatus;

            if (AssignedTo is null)
                return TaskErrors.NullAssignee;

            AssignedTo = null;
            AssignBy = null;

            return Result.Success();
        }

        public Result Start()
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (Status != Status.Pending)
                return TaskErrors.InvalidStatus;

            Status = Status.Ongoing;

            return Result.Success();
        }


        public Result Complete(Guid completedBy) =>
            Complete(completedBy, SystemConstants.UtcNow);

        public Result Complete(Guid completedBy, DateTime nowUtc)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (Status != Status.Ongoing)
                return TaskErrors.InvalidStatus;

            if (completedBy == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(completedBy));

            Status = Status.Completed;
            CompletedAtUtc = nowUtc;
            MarkedCompletedBy = completedBy;

            return Result.Success();
        }

        public Result Reopen()
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (Status != Status.Completed)
                return TaskErrors.InvalidStatus;

            Status = Status.Pending;
            CompletedAtUtc = null;
            MarkedCompletedBy = null;

            return Result.Success();
        }

        public Result UpdateDetails(
           Name title,
           DateTime dueDate,
           DateTime nowUtc)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (Status == Status.Completed)
                return TaskErrors.InvalidStatus;

            if (dueDate <= nowUtc)
                return TaskErrors.InvalidDueDate;

            Title = title;
            DueDate = dueDate;

            return Result.Success();
        }

        public Result UpdateDescription(string? description)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            Description = NormalizeDescription(description);

            return Result.Success();
        }
        public Result UpdatePriority(Priority priority)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (!Enum.IsDefined(priority))
                return TaskErrors.InvalidPriority;

            TaskPriority = priority;

            return Result.Success();
        }
        public Result Delete(Guid deletedBy, DateTime nowUtc)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (deletedBy == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(deletedBy));

            IsDeleted = true;
            DeletedAtUtc = nowUtc;
            DeletedBy = deletedBy;

            return Result.Success();
        }

        public Result EditComment(
            Guid commentId,
            Guid actorId,
            CommentBody body,
            DateTime nowUtc)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (actorId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(actorId));

            var comment = _comments.FirstOrDefault(
                c => c.Id == commentId && !c.IsDeleted);

            if (comment is null)
                return TaskCommentErrors.CommentNotFound;

            if (comment.AuthorId != actorId)
                return TaskCommentErrors.CommentNotOwnedByUser;

            return comment.Edit(body, nowUtc);
        }

        public Result DeleteComment(
            Guid commentId,
            Guid actorId,
            DateTime nowUtc)
        {
            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (actorId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(actorId));

            var comment = _comments.FirstOrDefault(
                c => c.Id == commentId && !c.IsDeleted);

            if (comment is null)
                return TaskCommentErrors.CommentNotFound;

            if (comment.AuthorId != actorId)
                return TaskCommentErrors.CommentNotOwnedByUser;

            return comment.Delete(nowUtc);
        }

        /// <remarks>
        /// The application layer must verify that the author
        /// is a member of this task's workspace.
        /// </remarks>
        public Result AddComment(
            Guid authorId,
            CommentBody body)
        {
            var nowUtc = SystemConstants.UtcNow;

            if (IsDeleted)
                return TaskErrors.TaskDeleted;

            if (authorId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(authorId));

            var result = Comment.Create(authorId, body, nowUtc);

            if (result.IsFailure)
                return result.Error;

            _comments.Add(result.value);

            return Result.Success();
        }

        /// <summary>Overdue is a pure function of stored status, due date and the clock. Never persisted.</summary>
        public Status GetEffectiveStatus(DateTime nowUtc)
        {
            if ((Status is Status.Pending or Status.Ongoing)
                && DueDate < nowUtc)
            {
                return Status.Overdue;
            }

            return Status;
        }

        public bool IsOverdue(DateTime nowUtc) =>
            GetEffectiveStatus(nowUtc) == Status.Overdue;

        private static string? NormalizeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            return description.Trim();
        }
    }
}
