using Application.Abstractions.Auth;
using Domain.Aggregates.User;
using Domain.Aggregates.User.Events;
using Domain.Shared.DomainEvent;

namespace Application.Features.Auth.Events;

public class PasswordChangedEmailHandler(
    IEmailService emailService,
    IUserRepository userRepository)
    : IDomainEventHandler<PasswordChanged>
{
    public async Task Handle(PasswordChanged domainEvent, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);

        await emailService.SendPasswordChangedEmailAsync(
            domainEvent.Email,
            user?.FirstName.Value ?? "there",
            cancellationToken);
    }
}
