using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result>;