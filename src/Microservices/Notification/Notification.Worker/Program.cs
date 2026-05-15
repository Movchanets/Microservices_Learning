using MassTransit;
using Marketplace.ServiceDefaults;
using Microsoft.AspNetCore.SignalR;
using Notification.Worker.Consumers;
using Notification.Worker.Hubs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── SignalR with Redis Backplane ────────────────────────
builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("redis")!,
        options =>
        {
            options.Configuration.ChannelPrefix =
                RedisChannel.Literal("marketplace");
        });

// ── Custom UserIdProvider (maps x-buyer-id header) ─────
builder.Services.AddSingleton<IUserIdProvider, BuyerIdUserIdProvider>();

// ── MassTransit v8 + Consumers ──────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<OrderCompletedConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// ── Map SignalR Hub ─────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");

// ── Health / Alive endpoints ────────────────────────────
app.MapDefaultEndpoints();

app.Run();
