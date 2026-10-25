namespace Application.Constants;

public static class AuthConstants
{
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);
}