using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
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
        // Emit one role claim per flag so RequireRole("Admin") works with multi-role users.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        // Add individual role claims for each flag that is set
        foreach (var roleValue in Enum.GetValues<UserRole>())
        {
            if (roleValue != UserRole.None && user.Role.HasFlag(roleValue))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleValue.ToString()));
            }
        }

        // Include StoreId for sellers so downstream services can verify store ownership.
        if (user.StoreId.HasValue)
        {
            claims.Add(new Claim("StoreId", user.StoreId.Value.ToString()));
        }

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
