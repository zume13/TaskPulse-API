using Application.Abstractions.Auth;
using Domain.Aggregates.User.Events;
using SharedKernel.DomainEvent;

namespace Application.Features.Auth.Events;

public class SendVerificationEmailHandler(IEmailService emailService) : IDomainEventHandler<UserRegistered>
{
    public async Task Handle(UserRegistered domainEvent, CancellationToken cancellationToken)
    {
        await emailService.SendVerificationEmailAsync(
            domainEvent.UserId,
            domainEvent.Email,
            domainEvent.FirstName,
            domainEvent.VerificationToken,
            cancellationToken);
    }
}