namespace Application.Abstractions.Auth;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);