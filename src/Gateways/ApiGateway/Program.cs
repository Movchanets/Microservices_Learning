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

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── YARP Reverse Proxy ──────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ── Authentication (Cookie session + dev JWT for seeder) ──
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Seller", policy => policy.RequireRole("Seller", "Admin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
// ── Named HTTP clients for BFF + health probing ─────────
builder.Services.AddHttpClient("identity-api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Identity:ApiBaseUrl"] ?? "http://identity-api");
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

// ── BFF Services ────────────────────────────────────────
builder.Services.AddScoped<CartBffService>();
builder.Services.AddScoped<OrderBffService>();

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

// ── Rate Limiting ───────────────────────────────────────
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

// ── OpenAPI (for Scalar — exposes Gateway/Health endpoints) ───
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

// ── Middleware pipeline ─────────────────────────────────
app.MapDefaultEndpoints(); // health checks
app.UseRateLimiter();
app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});
app.UseAuthentication();
app.UseCookieToBearer();   // Cookie → Bearer transform
app.UseCsrfValidation();   // Custom CSRF check (skips when Bearer present — safe because CookieToBearer already validated the session)
app.UseAuthorization();

app.MapBffEndpoints();
app.MapHealthEndpoints();
app.MapOpenApi();  // exposes /openapi/v1.json for Scalar

// ── YARP Proxy ──────────────────────────────────────────
app.MapReverseProxy();

app.Run();
