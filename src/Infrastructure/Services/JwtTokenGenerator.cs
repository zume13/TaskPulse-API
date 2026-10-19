using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstractions.Auth;
using Domain.Aggregates.User;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Constants;

namespace Infrastructure.Services;

public class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public AccessTokenResult GenerateAccessToken(User user)
    {
        var secret = GetRequiredSetting("Jwt:Secret");
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");
        var lifetimeMinutes = int.Parse(GetRequiredSetting("Jwt:AccessTokenExpirationMinutes"));

        var nowUtc = SystemConstants.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(lifetimeMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var tokenText = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(tokenText, expiresAtUtc);
    }

    private string GetRequiredSetting(string key)
    {
        return configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is missing.");
    }
}