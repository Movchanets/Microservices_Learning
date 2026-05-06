using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Service responsible for generating JWT access tokens and opaque refresh tokens.
/// </summary>
public sealed class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JSON Web Token (JWT) for the specified user.
    /// </summary>
    /// <param name="user">The user for whom to generate the token.</param>
    /// <returns>A signed JWT string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Jwt:Secret configuration is missing.</exception>
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret not configured")));

        // Rationale: Include essential claims (Sub, Email, Role) to allow downstream services
        // to authorize actions without querying the Identity service.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "marketplace-identity",
            audience: configuration["Jwt:Audience"] ?? "marketplace-api",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure random string to be used as a refresh token.
    /// Rationale: Generates 64 random bytes and converts to base64, providing 512 bits of entropy to prevent token guessing.
    /// </summary>
    /// <returns>A secure, opaque token string.</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
