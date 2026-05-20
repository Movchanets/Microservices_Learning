using Cart.API.Endpoints;
using Cart.Domain.Aggregates;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Repositories;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Messaging.Consumers;
using Cart.Application.Commands;
using Cart.Infrastructure.Serialization;
using FluentValidation;
using BuildingBlocks.Infrastructure.Middleware;
using MassTransit;
using Marketplace.ServiceDefaults;
using BuildingBlocks.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register ShoppingCartJsonConverter globally for HTTP responses
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new ShoppingCartJsonConverter());
});

// PostgreSQL DB
// NOTE: Do NOT use AddNpgsqlDbContext here — it uses AddDbContextPool internally,
// which conflicts with IDbContextOptionsConfiguration<T> being scoped in EF Core 10.
builder.Services.AddDbContext<CartDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("cart-db"),
        npgsql => npgsql.MigrationsAssembly(typeof(CartDbContext).Assembly.FullName));
});

// Redis
builder.AddRedisDistributedCache("redis");

builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IProductPriceRepository, ProductPriceRepository>();

// Application (MediatR + FluentValidation)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CheckoutCartCommand).Assembly);
});
builder.Services.AddValidatorsFromAssembly(typeof(CheckoutCartCommand).Assembly);

// MassTransit
builder.Services.AddMassTransit(x =>
{
    // Product price sync consumers from Catalog events
    x.AddConsumer<ProductCreatedConsumer>();
    x.AddConsumer<ProductUpdatedConsumer>();
    x.AddConsumer<ProductPriceChangedConsumer>();
    x.AddConsumer<ProductDeletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(connectionString);
        }

        cfg.ConfigureEndpoints(context);
    });
});

// ── Authentication ─────────────────────────────────────
builder.Services.AddMarketplaceAuthentication(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Drop & re-create DB in dev to handle replaced migrations
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    db.Database.EnsureDeleted();
    db.Database.Migrate();
}

app.MapCartEndpoints();

app.Run();