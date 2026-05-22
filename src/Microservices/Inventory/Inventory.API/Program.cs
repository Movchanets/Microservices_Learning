using BuildingBlocks.Infrastructure.Database.Interceptors;
using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Repositories;
using Inventory.API.Endpoints;
using MassTransit;
using Marketplace.ServiceDefaults;
using BuildingBlocks.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Infrastructure
// NOTE: Do NOT use AddNpgsqlDbContext here — it uses AddDbContextPool internally,
// which conflicts with IDbContextOptionsConfiguration<T> being scoped in EF Core 10.
builder.Services.AddSingleton<DomainEventDispatcherInterceptor>();
builder.Services.AddDbContext<InventoryDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("inventory-db"),
        npgsql => npgsql.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName));
    options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
});

builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());

// Application (MediatR)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ReserveStockCommand).Assembly);
});

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<InventoryDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<ReserveInventoryConsumer>();
    x.AddConsumer<CancelReservationConsumer>();
    x.AddConsumer<ProductCreatedConsumer>();
    x.AddConsumer<ProductUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(connectionString);
        }

        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("inventory", false));
    });
});

// ── Authentication ─────────────────────────────────────
builder.Services.AddMarketplaceAuthentication(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapInventoryEndpoints();

app.ApplyMigrations();

app.Run();
