using FluentValidation;

namespace Application.Features.Workspace.Commands.Rename;

public class RenameWorkspaceCommandValidator : AbstractValidator<RenameWorkspaceCommand>
{
    public RenameWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace id is required.");

        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MaximumLength(100).WithMessage("Workspace name cannot exceed 100 characters.");
    }
}