using FluentValidation;

namespace Application.Features.Workspace.Queries.GetWorkspace;

public class GetWorkspaceQueryValidator : AbstractValidator<GetWorkspaceQuery>
{
    public GetWorkspaceQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty().WithMessage("Workspace id is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
    }
}
