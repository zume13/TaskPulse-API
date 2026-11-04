using FluentValidation;

namespace Application.Features.Workspace.Commands.Archive;

public class ArchiveWorkspaceCommandValidator : AbstractValidator<ArchiveWorkspaceCommand>
{
    public ArchiveWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace id is required.");

        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("User id is required.");
    }
}