using BuildingBlocks.Infrastructure.Behaviors;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using MassTransit;
using Media.API.Endpoints;
using Media.API.Infrastructure;
using Media.API.Infrastructure.Persistence;
using Media.API.Services;
using Marketplace.ServiceDefaults;
using MediatR;
using BuildingBlocks.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Azure Blob Storage (Aspire — uses Azurite locally, Azure Storage in cloud)
builder.AddAzureBlobServiceClient("blobs");

// ── Media Infrastructure (DbContext, repos, storage) ────
builder.Services.AddMediaInfrastructure(builder.Configuration);

// ── Image processing service ────────────────────────────
builder.Services.AddScoped<ImageProcessingService>();

// ── MediatR + pipeline behaviors ────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.Lifetime = ServiceLifetime.Scoped;
    cfg.RegisterServicesFromAssembly(typeof(Media.API.Application.Commands.UploadMedia.UploadMediaCommand).Assembly);
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ── FluentValidation ────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(
    typeof(Media.API.Application.Commands.UploadMedia.UploadMediaValidator).Assembly);

// ── MassTransit + Outbox ────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<MediaDbContext>(o =>
    {
        o.UsePostgres();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});

// ── Authentication ─────────────────────────────────────
builder.Services.AddMarketplaceAuthentication(builder.Configuration);

// ── OpenAPI ─────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

app.ApplyMigrations();

// ── Middleware Pipeline ─────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

// ── Endpoints ───────────────────────────────────────────
app.MapMediaEndpoints();

app.Run();
