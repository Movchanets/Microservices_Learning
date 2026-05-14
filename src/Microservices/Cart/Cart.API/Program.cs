using Cart.API.Endpoints;
using Cart.Domain.Aggregates;
using Cart.Infrastructure.Repositories;
using Cart.Infrastructure.Data;
using Cart.Application.Commands;
using MassTransit;
using Marketplace.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// PostgreSQL DB
builder.AddNpgsqlDbContext<CartDbContext>("cart-db");

// Redis
builder.AddRedisDistributedCache("redis");

builder.Services.AddScoped<ICartRepository, CartRepository>();

// Application (MediatR)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CheckoutCartCommand).Assembly);
});

// MassTransit
builder.Services.AddMassTransit(x =>
{
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Auto-migrate on startup for dev environment
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    db.Database.Migrate();
}

app.MapCartEndpoints();

app.Run();