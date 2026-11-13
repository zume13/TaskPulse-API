using FluentValidation;

namespace Application.Features.Tasks.Commands.EditComment;

public class EditCommentCommandValidator : AbstractValidator<EditCommentCommand>
{
    public EditCommentCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task id is required.");

        RuleFor(x => x.CommentId)
            .NotEmpty().WithMessage("Comment id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment cannot be empty.")
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
    }
}