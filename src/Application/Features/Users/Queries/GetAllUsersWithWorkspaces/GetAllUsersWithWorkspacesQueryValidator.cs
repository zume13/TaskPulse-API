using FluentValidation;

namespace Application.Features.Users.Queries.GetAllUsersWithWorkspaces;

public class GetAllUsersWithWorkspacesQueryValidator : AbstractValidator<GetAllUsersWithWorkspacesQuery>
{
    public GetAllUsersWithWorkspacesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
