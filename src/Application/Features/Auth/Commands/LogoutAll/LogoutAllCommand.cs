using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.LogoutAll;

public record LogoutAllCommand(Guid UserId) : IRequest<Result>;