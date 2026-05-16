using System.Text;
using Cart.API.Endpoints;
using Cart.Domain.Aggregates;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Repositories;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Messaging.Consumers;
using Cart.Application.Commands;
using FluentValidation;
using MassTransit;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// PostgreSQL DB
builder.AddNpgsqlDbContext<CartDbContext>("cart-db");

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();

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