using FluentValidation;

namespace Application.Features.Tasks.Commands.Reopen;

public class ReopenTaskCommandValidator : AbstractValidator<ReopenTaskCommand>
{
    public ReopenTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}