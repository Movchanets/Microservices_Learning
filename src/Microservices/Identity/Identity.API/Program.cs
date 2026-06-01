using BuildingBlocks.Infrastructure.Behaviors;
using BuildingBlocks.Infrastructure.Database.Interceptors;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using Identity.API.Endpoints;
using Identity.Infrastructure;
using Identity.Infrastructure.Messaging.Consumers;
using Identity.Infrastructure.Persistence;
using MassTransit;
using Marketplace.ServiceDefaults;
using MediatR;
using BuildingBlocks.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Database ────────────────────────────────────────────
// NOTE: Do NOT use AddNpgsqlDbContext here — it uses AddDbContextPool internally,
// which conflicts with IDbContextOptionsConfiguration<T> being scoped in EF Core 10.
builder.Services.AddSingleton<DomainEventDispatcherInterceptor>();
builder.Services.AddDbContext<Identity.Infrastructure.Persistence.IdentityDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("identity-db"),
        npgsql => npgsql.MigrationsAssembly(typeof(Identity.Infrastructure.Persistence.IdentityDbContext).Assembly.FullName));
    options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
});

// ── Identity Infrastructure (repos, services) ───────────
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// ── MediatR + pipeline behaviors ────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Identity.Application.Commands.Register.RegisterUserCommand).Assembly));

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ── FluentValidation ────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(
    typeof(Identity.Application.Commands.Register.RegisterUserValidator).Assembly);

// ── MassTransit v8 + Outbox ─────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<StoreVerifiedConsumer>();
    x.AddConsumer<StoreCreatedConsumer>();

    x.AddEntityFrameworkOutbox<IdentityDbContext>(o =>
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Seller", policy => policy.RequireRole("Seller", "Admin"));
});

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
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();
