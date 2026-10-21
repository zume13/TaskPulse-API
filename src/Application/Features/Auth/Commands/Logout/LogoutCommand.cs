using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;