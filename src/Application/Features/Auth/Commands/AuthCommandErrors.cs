using Domain.Shared.ResultPattern;

namespace Application.Features.Auth.Commands
{
    public static class AuthCommandErrors
    {
        public static class ChangePasswordErrors
        {
            public static Error InvalidCurrentPassword => Error.Failure("ChangePassword.InvalidCurrentPassword", "The current password is incorrect.");
        }
        public static class LoginErrors
        {
            public static Error InvalidCredentials => Error.Unauthorized("Login.InvalidCredentials", "Email or password is incorrect.");

            public static Error EmailNotVerified => Error.Forbidden("Login.EmailNotVerified", "Please verify your email before logging in.");
        }
        public static class RegisterErrors
        {
            public static Error EmailAlreadyExists => Error.Conflict("Register.EmailAlreadyExists", "An account with this email already exists.");
        }
        public static class ResetPasswordErrors
        {
            public static Error InvalidToken => Error.Failure("ResetPassword.InvalidToken", "The password reset link is invalid or has expired.");
        }
    }

}
