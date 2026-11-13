using FluentValidation;

namespace Application.Features.Tasks.Commands.RemoveDocumentLink;

public class RemoveDocumentLinkCommandValidator : AbstractValidator<RemoveDocumentLinkCommand>
{
    public RemoveDocumentLinkCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}