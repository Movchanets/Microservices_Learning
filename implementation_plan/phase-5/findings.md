# Phase 5 — Findings

## Existing Infrastructure
- Redis resource declared in AppHost: `builder.AddRedis("redis").WithRedisInsight()`
- RabbitMQ resource declared: `builder.AddRabbitMQ("messaging").WithManagementPlugin()`
- YARP route for `/hubs/notifications/**` already in gateway `appsettings.json`
- YARP cluster `notificationCluster` with `SessionAffinity` (HashCookie) already configured
- AppHost placeholder: `// Phase 5: Notification  → .WithReference(redis).WithReference(messaging)`

## Contracts Available (from Phase 4)
- `OrderCompletedEvent(CorrelationId, OrderId, BuyerId)` — has BuyerId ✓
- `OrderCancelledEvent(CorrelationId, OrderId, BuyerId, Reason)` — has BuyerId ✓
- `PaymentFailedEvent(CorrelationId, OrderId, FailureReason)` — missing BuyerId ✗
- `InventoryReservationFailedEvent(CorrelationId, OrderId, Reason)` — missing BuyerId ✗

## MassTransit v8 Consumer Pattern (from Inventory/Payment reference)
- `x.SetKebabCaseEndpointNameFormatter()` — endpoint naming
- `x.AddConsumer<TConsumer>()` — register consumers
- `cfg.ConfigureEndpoints(context)` — auto-configure endpoints
- `cfg.Host(builder.Configuration.GetConnectionString("messaging"))` — RabbitMQ host

## SignalR Redis Backplane Pattern
```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(connectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("marketplace");
    });
```
