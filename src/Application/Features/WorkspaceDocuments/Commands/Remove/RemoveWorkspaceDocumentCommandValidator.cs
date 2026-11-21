using FluentValidation;

namespace Application.Features.WorkspaceDocuments.Commands.Remove;

public class RemoveWorkspaceDocumentCommandValidator : AbstractValidator<RemoveWorkspaceDocumentCommand>
{
    public RemoveWorkspaceDocumentCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
