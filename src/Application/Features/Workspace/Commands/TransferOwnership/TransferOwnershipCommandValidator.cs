using FluentValidation;

namespace Application.Features.Workspace.Commands.TransferOwnership;

public class TransferOwnershipCommandValidator : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace id is required.");

        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.NewOwnerId)
            .NotEmpty().WithMessage("New owner id is required.");
    }
}