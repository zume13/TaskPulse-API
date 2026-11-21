using Domain.Aggregates.WorkspaceDocuments;
using FluentValidation;

namespace Application.Features.WorkspaceDocuments.Commands.Register;

public class RegisterWorkspaceDocumentCommandValidator : AbstractValidator<RegisterWorkspaceDocumentCommand>
{
    public RegisterWorkspaceDocumentCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).Must(WorkspaceDocument.IsValidTitle).WithMessage("A document title of at most 150 characters is required.");
        RuleFor(x => x.GoogleFileId).Must(WorkspaceDocument.IsValidGoogleFileId).WithMessage("A canonical Google file ID is required, not a URL.");
        RuleFor(x => x.SourceType).IsInEnum();
        RuleFor(x => x.MimeType).Must((request, mimeType) => WorkspaceDocument.IsValidSource(request.SourceType, mimeType))
            .WithMessage("Manual links accept no MIME type or the Google Docs MIME type; uploads require the DOCX MIME type.");
    }
}
