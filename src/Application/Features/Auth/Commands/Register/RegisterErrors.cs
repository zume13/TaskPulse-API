using Domain.Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Register;

public static class RegisterErrors
{
    public static Error EmailAlreadyExists => Error.Conflict("Register.EmailAlreadyExists", "An account with this email already exists.");
}