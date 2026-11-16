using Domain.Aggregates.LoginAudit;
using FluentValidation;

namespace Application.Features.Auth.Queries.GetLoginAudits;

public class GetLoginAuditsQueryValidator : AbstractValidator<GetLoginAuditsQuery>
{
    public GetLoginAuditsQueryValidator()
    {
        RuleFor(x => x.Email)
            .MaximumLength(LoginAudit.MaxEmailLength).WithMessage("Email filter is too long.");

        RuleFor(x => x.Outcome)
            .IsInEnum().WithMessage("Invalid login outcome.");

        RuleFor(x => x)
            .Must(x => x.ToUtc > x.FromUtc)
            .When(x => x.FromUtc.HasValue && x.ToUtc.HasValue)
            .WithMessage("'to' must be later than 'from'.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}