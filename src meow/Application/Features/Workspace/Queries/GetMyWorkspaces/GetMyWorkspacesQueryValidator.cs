using FluentValidation;

namespace Application.Features.Workspace.Queries.GetMyWorkspaces;

public class GetMyWorkspacesQueryValidator : AbstractValidator<GetMyWorkspacesQuery>
{
    public GetMyWorkspacesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}
