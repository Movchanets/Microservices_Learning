using ApiGateway.Middleware;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── YARP Reverse Proxy ──────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ── Authentication (Cookie session + OIDC) ──────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.Name = "Marketplace.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddOpenIdConnect("oidc", options =>
{
    // These will be configured via Aspire service discovery
    options.Authority = builder.Configuration["Identity:Authority"] ?? "http://identity-api";
    options.ClientId = "marketplace-bff";
    options.ClientSecret = builder.Configuration["Identity:ClientSecret"] ?? "bff-secret";
    options.ResponseType = "code";
    options.SaveTokens = true; // Store tokens server-side in session
    options.GetClaimsFromUserInfoEndpoint = true;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access"); // Refresh tokens
});

builder.Services.AddAuthorization();

// ── CORS (for Angular SPA) ──────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4200"])
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
app.MapGet("/bff/login", () => Results.Challenge(
    new() { RedirectUri = "/" },
    ["oidc"]))
    .ExcludeFromDescription();

app.MapGet("/bff/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync("Cookies");
    await ctx.SignOutAsync("oidc");
    return Results.Redirect("/");
})
.ExcludeFromDescription();

app.MapGet("/bff/user", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        email = ctx.User.FindFirst("email")?.Value,
        name = ctx.User.Identity.Name,
        role = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });
})
.ExcludeFromDescription();

// ── CSRF token endpoint ─────────────────────────────────
app.MapGet("/bff/csrf", (HttpContext ctx) =>
{
    var token = Guid.NewGuid().ToString("N");
    ctx.Response.Cookies.Append("XSRF-TOKEN", token, new CookieOptions
    {
        HttpOnly = false, // Angular must read this
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/"
    });
    return Results.Ok();
});

// ── YARP Proxy ──────────────────────────────────────────
app.MapReverseProxy();

app.Run();

// ── Extension methods for middleware registration ───────
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCookieToBearer(this IApplicationBuilder app) =>
        app.UseMiddleware<CookieToBearerMiddleware>();

    public static IApplicationBuilder UseCsrfValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<CsrfValidationMiddleware>();
}
