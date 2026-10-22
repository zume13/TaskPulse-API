using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.ResendVerification;

public record ResendVerificationCommand(string Email) : IRequest<Result>;