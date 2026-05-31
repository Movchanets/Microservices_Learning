using System.Text;
using ApiGateway.Endpoints;
using ApiGateway.Extensions;
using ApiGateway.Middleware;
using ApiGateway.Services;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ════════════════════════════════════════════════════════════════
// 1. ASPIRE SERVICE DEFAULTS
//    Telemetry, health checks, resilience policies
// ════════════════════════════════════════════════════════════════
builder.AddServiceDefaults();

// ════════════════════════════════════════════════════════════════
// 2. YARP REVERSE PROXY
//    Routes /api/* to downstream microservices via service discovery
// ════════════════════════════════════════════════════════════════
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ════════════════════════════════════════════════════════════════
// 3. AUTHENTICATION
//    Production: Cookie-based sessions for SPA
//    Development: Cookie + JWT Bearer (for Seeder.App and testing)
//
//    Pipeline: CookieToBearerMiddleware transforms cookie → Bearer
//    before YARP proxies requests to downstream services.
// ════════════════════════════════════════════════════════════════
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = builder.Environment.IsDevelopment()
        ? "DevPolicyScheme"
        : "Cookies";
    options.DefaultChallengeScheme = builder.Environment.IsDevelopment()
        ? "DevPolicyScheme"
        : "Cookies";
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

// In dev, also accept JWT Bearer tokens (e.g. from Seeder.App).
// Production traffic is cookie-only via the SPA.
if (builder.Environment.IsDevelopment())
{
    authBuilder
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
            };
        })
        .AddPolicyScheme("DevPolicyScheme", "Cookie or Bearer", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var auth = context.Request.Headers.Authorization.ToString();
                return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? JwtBearerDefaults.AuthenticationScheme
                    : "Cookies";
            };
        });
}

// ════════════════════════════════════════════════════════════════
// 4. AUTHORIZATION POLICIES
//    Named policies used by endpoints via .RequireAuthorization()
// ════════════════════════════════════════════════════════════════
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Seller", policy => policy.RequireRole("Seller", "Admin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ════════════════════════════════════════════════════════════════
// 5. NAMED HTTP CLIENTS
//    One per downstream microservice — used by BFF services
//    Service discovery resolves hostnames to actual addresses
// ════════════════════════════════════════════════════════════════
builder.Services.AddHttpClient("identity-api", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Identity:ApiBaseUrl"] ?? "http://identity-api");
});
builder.Services.AddHttpClient("catalog-api", c => c.BaseAddress = new Uri("http://catalog-api"));
builder.Services.AddHttpClient("ordering-api", c => c.BaseAddress = new Uri("http://ordering-api"));
builder.Services.AddHttpClient("inventory-api", c => c.BaseAddress = new Uri("http://inventory-api"));
builder.Services.AddHttpClient("cart-api", c => c.BaseAddress = new Uri("http://cart-api"));
builder.Services.AddHttpClient("search-api", c => c.BaseAddress = new Uri("http://search-api"));
builder.Services.AddHttpClient("store-api", c => c.BaseAddress = new Uri("http://store-api"));
builder.Services.AddHttpClient("media-api", c => c.BaseAddress = new Uri("http://media-api"));
builder.Services.AddHttpClient("payment-api", c => c.BaseAddress = new Uri("http://payment-api"));
builder.Services.AddHttpClient("notification-worker", c => c.BaseAddress = new Uri("http://notification-worker"));

// ════════════════════════════════════════════════════════════════
// 6. BFF SERVICES
//    Aggregation services that merge data from multiple microservices
// ════════════════════════════════════════════════════════════════
builder.Services.AddScoped<CartBffService>();
builder.Services.AddScoped<OrderBffService>();
builder.Services.AddScoped<ProductBffService>();

// ════════════════════════════════════════════════════════════════
// 7. CORS
//    Allows Angular SPA (localhost:4200) to call BFF endpoints
// ════════════════════════════════════════════════════════════════
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

// ════════════════════════════════════════════════════════════════
// 8. RATE LIMITING
//    Fixed window: 100 requests/minute, queue up to 10
// ════════════════════════════════════════════════════════════════
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ════════════════════════════════════════════════════════════════
// 9. OPENAPI
//    Exposes /openapi/v1.json for Scalar UI (Gateway + Health endpoints only)
// ════════════════════════════════════════════════════════════════
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "API Gateway";
        doc.Info.Description = "BFF aggregation layer — health probes and session management.";
        doc.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// ════════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE
// Order matters — each middleware runs top-to-bottom
// ════════════════════════════════════════════════════════════════

app.MapDefaultEndpoints();           // Health checks (/health, /alive)
app.UseRateLimiter();                // Rate limiting (100 req/min)
app.UseCors();                       // CORS for Angular SPA

app.UseMiddleware<RequestLoggingMiddleware>();

// Enable request body buffering so YARP can re-read the body
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseAuthentication();

// CookieToBearer MUST run BEFORE CsrfValidation:
// CSRF checks !hasBearerToken — if CookieToBearer hasn't set it yet,
// all cookie-based API calls get 403 CSRF errors.
app.UseCookieToBearer();             // Cookie → Bearer transform
app.UseCsrfValidation();             // Custom CSRF check (skips when Bearer present)
app.UseAuthorization();

// ── Endpoint Registration ────────────────────────────────────
app.MapBffEndpoints();               // /bff/* — session, cart, orders, catalog
app.MapHealthEndpoints();            // /bff/health — aggregated health
app.MapOpenApi();                    // /openapi/v1.json

// ── YARP Proxy (must be last — catches all /api/* routes) ────
app.MapReverseProxy();

app.Run();
