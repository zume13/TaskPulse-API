using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;