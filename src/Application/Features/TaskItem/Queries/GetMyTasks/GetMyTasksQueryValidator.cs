using FluentValidation;

namespace Application.Features.Tasks.Queries.GetMyTasks;

public class GetMyTasksQueryValidator : AbstractValidator<GetMyTasksQuery>
{
    public GetMyTasksQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid task status.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid task priority.");

        RuleFor(x => x.SortBy)
            .IsInEnum().WithMessage("Invalid sort field.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search text cannot exceed 100 characters.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
