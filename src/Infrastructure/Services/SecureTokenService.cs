using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Auth;

namespace Infrastructure.Services;

public class SecureTokenService : ISecureTokenService
{
    public string GenerateToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
    }

    public string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}