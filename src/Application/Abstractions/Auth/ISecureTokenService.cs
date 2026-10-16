namespace Application.Abstractions.Auth;

public interface ISecureTokenService
{
    string GenerateToken();

    string Hash(string token);
}