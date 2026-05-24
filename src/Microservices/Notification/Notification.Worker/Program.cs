using BuildingBlocks.Infrastructure.Authentication;
using MassTransit;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.SignalR;
using Notification.Worker.Consumers;
using Notification.Worker.Hubs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Authentication ─────────────────────────────────────
builder.Services.AddMarketplaceAuthentication(builder.Configuration);

// ── SignalR with Redis Backplane ────────────────────────
builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("redis")!,
        options =>
        {
            options.Configuration.ChannelPrefix =
                RedisChannel.Literal("marketplace");
        })
    .AddHubOptions<NotificationHub>(options =>
    {
        // Increase client timeout to 60s to tolerate proxy/WebSocket latency.
        // Default is 30s — clients behind gateways may need more time.
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
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
