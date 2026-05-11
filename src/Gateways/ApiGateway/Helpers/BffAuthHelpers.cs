using ApiGateway.Contracts;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Helpers;

internal static class BffAuthHelpers
{
    internal static async Task SignInAsync(HttpContext context, AuthResponse authResponse)
    {
        var payload = ReadJwtPayload(authResponse.AccessToken);
        var claims = new List<Claim>();

        AddClaimIfPresent(claims, ClaimTypes.NameIdentifier, GetPayloadValue(payload, "sub"));
        AddClaimIfPresent(claims, "sub", GetPayloadValue(payload, "sub"));
        AddClaimIfPresent(claims, ClaimTypes.Email, GetPayloadValue(payload, "email"));
        AddClaimIfPresent(claims, "email", GetPayloadValue(payload, "email"));
        AddClaimIfPresent(claims, ClaimTypes.Role, GetPayloadValue(payload, ClaimTypes.Role));
        AddClaimIfPresent(claims, "firstName", GetPayloadValue(payload, "firstName"));
        AddClaimIfPresent(claims, "lastName", GetPayloadValue(payload, "lastName"));

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = authResponse.ExpiresAt
        };

        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = authResponse.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = authResponse.RefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = authResponse.ExpiresAt.ToString("O") }
        ]);

        await context.SignInAsync("Cookies", principal, properties);
    }

    internal static void IssueCsrfCookie(HttpContext context)
    {
        context.Response.Cookies.Append("XSRF-TOKEN", Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = false,
            Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    internal static async Task<IResult> ToProblemResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var payload = await response.Content.ReadAsStringAsync(ct);
        return Results.Text(payload, "application/json", statusCode: (int)response.StatusCode);
    }

    private static void AddClaimIfPresent(List<Claim> claims, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }

    private static JsonElement ReadJwtPayload(string token)
    {
        var segments = token.Split('.');
        if (segments.Length < 2)
        {
            throw new InvalidOperationException("Invalid JWT token format.");
        }

        var json = Encoding.UTF8.GetString(Base64UrlDecode(segments[1]));
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padding = 4 - normalized.Length % 4;
        if (padding is > 0 and < 4)
        {
            normalized = normalized.PadRight(normalized.Length + padding, '=');
        }

        return Convert.FromBase64String(normalized);
    }

    private static string? GetPayloadValue(JsonElement payload, string claimName)
    {
        if (!payload.TryGetProperty(claimName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }
}
