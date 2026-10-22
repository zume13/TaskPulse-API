using FluentValidation;

namespace Application.Features.Auth.Commands.LogoutAll;

public class LogoutAllCommandValidator : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}