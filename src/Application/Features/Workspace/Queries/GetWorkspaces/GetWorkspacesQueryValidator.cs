using FluentValidation;

namespace Application.Features.Workspace.Queries.GetWorkspaces;

public class GetWorkspacesQueryValidator : AbstractValidator<GetWorkspacesQuery>
{
    public GetWorkspacesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
    }
}
