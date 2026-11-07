using FluentValidation;

namespace Application.Features.Tasks.Commands.Assign;

public class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.AssigneeId)
            .NotEmpty().WithMessage("Assignee id is required.");
    }
}