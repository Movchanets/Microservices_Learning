using System.Text;
using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Repositories;
using Inventory.API.Endpoints;
using MassTransit;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Infrastructure
builder.AddNpgsqlDbContext<InventoryDbContext>("inventory-db");

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
    x.AddEntityFrameworkOutbox<InventoryDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<ReserveInventoryConsumer>();
    x.AddConsumer<CancelReservationConsumer>();
    x.AddConsumer<ProductCreatedConsumer>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapInventoryEndpoints();

if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
    app.SeedData();
}

app.Run();