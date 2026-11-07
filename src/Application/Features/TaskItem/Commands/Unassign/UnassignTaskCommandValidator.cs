using FluentValidation;

namespace Application.Features.Tasks.Commands.Unassign;

public class UnassignTaskCommandValidator : AbstractValidator<UnassignTaskCommand>
{
    public UnassignTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}