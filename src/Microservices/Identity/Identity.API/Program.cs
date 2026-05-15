using BuildingBlocks.Infrastructure.Behaviors;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using Identity.API.Endpoints;
using Identity.Infrastructure;
using Identity.Infrastructure.Messaging.Consumers;
using Identity.Infrastructure.Persistence;
using MassTransit;
using Marketplace.ServiceDefaults;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Aspire PostgreSQL integration ───────────────────────
builder.AddNpgsqlDbContext<Identity.Infrastructure.Persistence.IdentityDbContext>("identity-db", configureDbContextOptions: dbContextOptionsBuilder =>
{
    dbContextOptionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("identity-db"), npgsql =>
        npgsql.MigrationsAssembly(typeof(Identity.Infrastructure.Persistence.IdentityDbContext).Assembly.FullName));
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

    x.AddEntityFrameworkOutbox<IdentityDbContext>(o =>
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

// ── Authentication (JWT Bearer) ─────────────────────────
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "marketplace-identity",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "marketplace-api",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Secret"] ?? "super-secret-key-for-dev-only-min-32-chars!!")),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── OpenAPI ─────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

app.ApplyMigrations();

if (app.Environment.IsDevelopment())
{
    app.SeedData();
}

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
