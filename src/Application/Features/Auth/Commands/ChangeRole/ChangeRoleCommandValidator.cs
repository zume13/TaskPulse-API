using FluentValidation;

namespace Application.Features.Auth.Commands.ChangeRole;

public class ChangeRoleCommandValidator : AbstractValidator<ChangeRoleCommand>
{
    public ChangeRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.NewRole)
            .IsInEnum().WithMessage("Role is invalid.");
    }
}