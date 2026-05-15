using BuildingBlocks.Infrastructure.Middleware;
using Marketplace.ServiceDefaults;
using Media.API.Endpoints;
using Media.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Azure Blob Storage (Aspire — uses Azurite locally, Azure Storage in cloud)
builder.AddAzureBlobServiceClient("blobs");

// ── Image processing service ────────────────────────────
builder.Services.AddScoped<ImageProcessingService>();

// ── Authentication (JWT Bearer) ─────────────────────────
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
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
    });

builder.Services.AddAuthorization();

// ── OpenAPI ─────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware Pipeline ─────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ───────────────────────────────────────────
app.MapMediaEndpoints();

app.Run();
