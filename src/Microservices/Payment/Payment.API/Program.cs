using BuildingBlocks.Infrastructure.Behaviors;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using MassTransit;
using Marketplace.ServiceDefaults;
using MediatR;
using BuildingBlocks.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Payment.API.Endpoints;
using Payment.Application.Commands.ProcessPayment;
using Payment.Application.Commands.RefundPayment;
using Payment.Infrastructure;
using Payment.Infrastructure.Data;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Database ────────────────────────────────────────────
// NOTE: Do NOT use AddNpgsqlDbContext here — it uses AddDbContextPool internally,
// which conflicts with IDbContextOptionsConfiguration<T> being scoped in EF Core 10.
builder.Services.AddDbContext<PaymentDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("payment-db"),
        npgsql => npgsql.MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName));
});

// ── Payment Infrastructure ──────────────────────────────
builder.Services.AddPaymentInfrastructure();

// ── MediatR + pipeline behaviors ────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.Lifetime = ServiceLifetime.Scoped;
    cfg.RegisterServicesFromAssemblyContaining<ProcessPaymentInternalCommand>();
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddValidatorsFromAssemblyContaining<RefundPaymentValidator>();

// ── MassTransit v8 + Outbox ─────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<ProcessPaymentConsumer>();
    x.AddConsumer<RefundPaymentConsumer>();

    x.AddEntityFrameworkOutbox<PaymentDbContext>(o =>
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

// ── Middleware pipeline ─────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints(); // health checks
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

// ── Endpoints ───────────────────────────────────────────
app.MapPaymentEndpoints();

if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}

app.Run();
