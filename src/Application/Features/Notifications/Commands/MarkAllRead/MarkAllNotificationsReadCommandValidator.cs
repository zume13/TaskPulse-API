using FluentValidation;

namespace Application.Features.Notifications.Commands.MarkAllRead;

public class MarkAllNotificationsReadCommandValidator : AbstractValidator<MarkAllNotificationsReadCommand>
{
    public MarkAllNotificationsReadCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}