using BuildingBlocks.Infrastructure.Behaviors;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using MassTransit;
using Marketplace.ServiceDefaults;
using MediatR;
using Ordering.API.Endpoints;
using Ordering.API.Saga;
using Ordering.Application.Commands.CreateOrder;
using Ordering.Infrastructure.Messaging.Consumers;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Database ────────────────────────────────────────────
builder.AddNpgsqlDbContext<OrderingDbContext>("ordering-db");

// ── Ordering Infrastructure ─────────────────────────────
builder.Services.AddOrderingInfrastructure();

// ── MediatR + pipeline behaviors ────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.Lifetime = ServiceLifetime.Scoped;
    cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ── FluentValidation ────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

// ── MassTransit v8 + Saga + Outbox ──────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // Consumer that creates Order entity from OrderSubmittedEvent
    x.AddConsumer<OrderSubmittedConsumer>();

    // Saga state machine with EF Core persistence
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrderingDbContext>();
            r.UsePostgres();
        });

    // Outbox for reliable event publishing
    x.AddEntityFrameworkOutbox<OrderingDbContext>(o =>
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

// ── OpenAPI ─────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints(); // health checks
app.MapOpenApi();

// ── Endpoints ───────────────────────────────────────────
app.MapOrderEndpoints();

if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}

app.Run();
