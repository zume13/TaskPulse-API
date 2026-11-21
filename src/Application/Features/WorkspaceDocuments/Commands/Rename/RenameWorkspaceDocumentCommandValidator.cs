using Domain.Aggregates.WorkspaceDocuments;
using FluentValidation;

namespace Application.Features.WorkspaceDocuments.Commands.Rename;

public class RenameWorkspaceDocumentCommandValidator : AbstractValidator<RenameWorkspaceDocumentCommand>
{
    public RenameWorkspaceDocumentCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).Must(WorkspaceDocument.IsValidTitle).WithMessage("A document title of at most 150 characters is required.");
    }
}
