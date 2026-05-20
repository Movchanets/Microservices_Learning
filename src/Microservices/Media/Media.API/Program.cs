using BuildingBlocks.Infrastructure.Middleware;
using Marketplace.ServiceDefaults;
using Media.API.Endpoints;
using Media.API.Services;
using BuildingBlocks.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Azure Blob Storage (Aspire — uses Azurite locally, Azure Storage in cloud)
builder.AddAzureBlobServiceClient("blobs");

// ── Image processing service ────────────────────────────
builder.Services.AddScoped<ImageProcessingService>();

// ── Authentication ─────────────────────────────────────
builder.Services.AddMarketplaceAuthentication(builder.Configuration);

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
