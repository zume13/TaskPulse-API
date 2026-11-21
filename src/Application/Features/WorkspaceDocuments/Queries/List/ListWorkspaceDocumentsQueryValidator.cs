using FluentValidation;

namespace Application.Features.WorkspaceDocuments.Queries.List;

public class ListWorkspaceDocumentsQueryValidator : AbstractValidator<ListWorkspaceDocumentsQuery>
{
    public ListWorkspaceDocumentsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
