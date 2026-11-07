using FluentValidation;

namespace Application.Features.Tasks.Commands.Start;

public class StartTaskCommandValidator : AbstractValidator<StartTaskCommand>
{
    public StartTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}