using FluentValidation;

namespace Application.Features.Tasks.Commands.Create;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title cannot exceed 100 characters.");

        RuleFor(x => x.DueDate)
            .NotEqual(default(DateTimeOffset)).WithMessage("Due date is required.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority is invalid.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
    }
}