using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Users.Commands.ChangeEmail;

public record ChangeEmailCommand(Guid UserId, string NewEmail, string CurrentPassword) : IRequest<Result>;