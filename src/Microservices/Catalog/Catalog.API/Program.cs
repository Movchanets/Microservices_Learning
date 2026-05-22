using BuildingBlocks.Infrastructure.Behaviors;
using BuildingBlocks.Infrastructure.Middleware;
using Catalog.API.Endpoints;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using FluentValidation;
using MassTransit;
using Marketplace.ServiceDefaults;
using MediatR;
using BuildingBlocks.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Catalog Infrastructure (repos, services) ────────────
builder.Services.AddCatalogInfrastructure(builder.Configuration);

// ── MediatR + pipeline behaviors ────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.Lifetime = ServiceLifetime.Scoped;
    cfg.RegisterServicesFromAssemblyContaining<Catalog.Application.Commands.CreateProduct.CreateProductCommand>();
    cfg.RegisterServicesFromAssemblyContaining<Catalog.Infrastructure.EventPublishing.ProductCreatedDomainEventHandler>();
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ── FluentValidation ────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<Catalog.Application.Commands.CreateProduct.CreateProductValidator>();

// ── MassTransit + Outbox ────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
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

// ── Middleware pipeline ─────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints(); // health checks
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

// ── Endpoints ───────────────────────────────────────────
app.MapProductEndpoints();
app.MapCategoryEndpoints();

app.Run();
