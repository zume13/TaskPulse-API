using FluentValidation;

namespace Application.Features.Tasks.Commands.SetDocumentLink;

public class SetDocumentLinkCommandValidator : AbstractValidator<SetDocumentLinkCommand>
{
    public SetDocumentLinkCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required.")
            .MaximumLength(2000).WithMessage("Url cannot exceed 2000 characters.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required.")
            .MaximumLength(200).WithMessage("Label cannot exceed 200 characters.");
    }
}