using System.Text;
using MassTransit;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Notification.Worker.Consumers;
using Notification.Worker.Hubs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Authentication (JWT Bearer) ─────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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

        // SignalR sends JWT via query string for WebSocket handshake
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── SignalR with Redis Backplane ────────────────────────
builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("redis")!,
        options =>
        {
            options.Configuration.ChannelPrefix =
                RedisChannel.Literal("marketplace");
        });

// ── Custom UserIdProvider (maps authenticated claims) ───
builder.Services.AddSingleton<IUserIdProvider, BuyerIdUserIdProvider>();

// ── MassTransit v8 + Consumers ──────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<OrderCompletedConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();
    x.AddConsumer<OrderStatusChangedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// ── Map SignalR Hub ─────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");

// ── Health / Alive endpoints ────────────────────────────
app.MapDefaultEndpoints();

app.Run();
