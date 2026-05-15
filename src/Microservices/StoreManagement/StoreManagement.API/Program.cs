using BuildingBlocks.Infrastructure.Behaviors;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using MassTransit;
using Marketplace.ServiceDefaults;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StoreManagement.API.Endpoints;
using StoreManagement.Application.Commands.CreateStore;
using StoreManagement.Infrastructure;
using StoreManagement.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── StoreManagement Infrastructure (repos, services) ───
builder.Services.AddStoreManagementInfrastructure(builder.Configuration);

// ── MediatR + pipeline behaviors ────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.Lifetime = ServiceLifetime.Scoped;
    cfg.RegisterServicesFromAssemblyContaining<CreateStoreCommand>();
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ── FluentValidation ────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<CreateStoreValidator>();

// ── MassTransit v8 + Outbox ─────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<StoreDbContext>(o =>
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
app.MapStoreEndpoints();

// ── Auto-migrate in development ─────────────────────────
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}

app.Run();
