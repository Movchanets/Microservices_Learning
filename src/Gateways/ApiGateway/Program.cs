using ApiGateway.Middleware;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── YARP Reverse Proxy ──────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ── Authentication (Cookie session) ─────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.Name = "Marketplace.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();
builder.Services.AddHttpClient("identity-api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Identity:ApiBaseUrl"] ?? "http://identity-api");
});

// ── CORS (for Angular SPA) ──────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4200", "http://localhost:4201"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────
app.MapDefaultEndpoints(); // health checks
app.UseCors();
app.UseAuthentication();
app.UseCsrfValidation();   // Custom CSRF check
app.UseCookieToBearer();   // Cookie → Bearer transform
app.UseAuthorization();

// ── BFF endpoints ───────────────────────────────────────
app.MapPost("/bff/auth/login", async (
    LoginRequest request,
    IHttpClientFactory httpClientFactory,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var http = httpClientFactory.CreateClient("identity-api");
    var response = await http.PostAsJsonAsync("/api/identity/auth/login", request, ct);

    if (!response.IsSuccessStatusCode)
    {
        return await ToProblemResultAsync(response, ct);
    }

    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
    if (authResponse is null)
    {
        return Results.Problem("Identity service returned an empty response.", statusCode: StatusCodes.Status502BadGateway);
    }

    await SignInAsync(ctx, authResponse);
    IssueCsrfCookie(ctx);

    return Results.NoContent();
})
.AllowAnonymous()
.ExcludeFromDescription();

app.MapPost("/bff/auth/register", async (
    RegisterRequest request,
    IHttpClientFactory httpClientFactory,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var http = httpClientFactory.CreateClient("identity-api");
    var response = await http.PostAsJsonAsync("/api/identity/auth/register", request, ct);

    if (!response.IsSuccessStatusCode)
    {
        return await ToProblemResultAsync(response, ct);
    }

    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
    if (authResponse is null)
    {
        return Results.Problem("Identity service returned an empty response.", statusCode: StatusCodes.Status502BadGateway);
    }

    await SignInAsync(ctx, authResponse);
    IssueCsrfCookie(ctx);

    return Results.NoContent();
})
.AllowAnonymous()
.ExcludeFromDescription();

app.MapPost("/bff/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync("Cookies");
    ctx.Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
    {
        Path = "/",
        SameSite = SameSiteMode.Strict,
        Secure = !app.Environment.IsDevelopment()
    });

    return Results.NoContent();
})
.RequireAuthorization()
.ExcludeFromDescription();

app.MapGet("/bff/user", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub"),
        email = ctx.User.FindFirstValue("email") ?? ctx.User.FindFirstValue(ClaimTypes.Email),
        firstName = ctx.User.FindFirstValue("firstName"),
        lastName = ctx.User.FindFirstValue("lastName"),
        role = ctx.User.FindFirstValue(ClaimTypes.Role)
    });
})
.RequireAuthorization()
.ExcludeFromDescription();

// ── CSRF token endpoint ─────────────────────────────────
app.MapGet("/bff/csrf", (HttpContext ctx) =>
{
    IssueCsrfCookie(ctx);
    return Results.Ok();
})
.AllowAnonymous()
.ExcludeFromDescription();

app.MapGet("/bff/health", async (IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    try
    {
        var http = httpClientFactory.CreateClient("identity-api");
        var probeResponse = await http.PostAsJsonAsync(
            "/api/identity/auth/login",
            new LoginRequest("healthcheck-probe@test.local", "InvalidPassword123!"),
            ct);

        var statusCode = (int)probeResponse.StatusCode;
        var isReady = statusCode < StatusCodes.Status500InternalServerError
            && statusCode != StatusCodes.Status404NotFound;

        return isReady
            ? Results.Ok(new { status = "Healthy" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
})
.AllowAnonymous()
.ExcludeFromDescription();

// ── YARP Proxy ──────────────────────────────────────────
app.MapReverseProxy();

app.Run();

static async Task SignInAsync(HttpContext context, AuthResponse authResponse)
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

static void IssueCsrfCookie(HttpContext context)
{
    context.Response.Cookies.Append("XSRF-TOKEN", Guid.NewGuid().ToString("N"), new CookieOptions
    {
        HttpOnly = false,
        Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/"
    });
}

static void AddClaimIfPresent(List<Claim> claims, string claimType, string? value)
{
    if (!string.IsNullOrWhiteSpace(value))
    {
        claims.Add(new Claim(claimType, value));
    }
}

static async Task<IResult> ToProblemResultAsync(HttpResponseMessage response, CancellationToken ct)
{
    var payload = await response.Content.ReadAsStringAsync(ct);
    return Results.Text(payload, "application/json", statusCode: (int)response.StatusCode);
}

static JsonElement ReadJwtPayload(string token)
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

static byte[] Base64UrlDecode(string value)
{
    var normalized = value.Replace('-', '+').Replace('_', '/');
    var padding = 4 - normalized.Length % 4;
    if (padding is > 0 and < 4)
    {
        normalized = normalized.PadRight(normalized.Length + padding, '=');
    }

    return Convert.FromBase64String(normalized);
}

static string? GetPayloadValue(JsonElement payload, string claimName)
{
    if (!payload.TryGetProperty(claimName, out var value))
    {
        return null;
    }

    return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
}

// ── Extension methods for middleware registration ───────
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCookieToBearer(this IApplicationBuilder app) =>
        app.UseMiddleware<CookieToBearerMiddleware>();

    public static IApplicationBuilder UseCsrfValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<CsrfValidationMiddleware>();
}

sealed record LoginRequest(string Email, string Password);
sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, string Email, string Role);
