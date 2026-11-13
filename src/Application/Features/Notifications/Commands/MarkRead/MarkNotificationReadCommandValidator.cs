using FluentValidation;

namespace Application.Features.Notifications.Commands.MarkRead;

public class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("Notification id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");
    }
}