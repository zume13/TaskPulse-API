using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result>;