# 07 — Real-time Communication: SignalR & WebSockets

## Overview

The marketplace delivers instant push notifications (order status, payment results) via **ASP.NET Core SignalR** through WebSocket connections, all routed via YARP.

## Architecture

```mermaid
graph TB
    subgraph "Client"
        ANG["Angular SPA<br/>SignalR Client"]
    end

    subgraph "Edge"
        YARP["YARP Gateway<br/>WebSocket Proxy"]
    end

    subgraph "Notification Cluster (N instances)"
        N1["Notification.Worker #1<br/>SignalR Hub"]
        N2["Notification.Worker #2<br/>SignalR Hub"]
    end

    subgraph "Backplane"
        REDIS["Redis Pub/Sub<br/>SignalR Backplane"]
    end

    subgraph "Message Bus"
        BUS["RabbitMQ / ASB"]
    end

    ANG <-->|WebSocket| YARP
    YARP <-->|Sticky Session| N1
    N1 <--> REDIS
    N2 <--> REDIS
    BUS --> N1
    BUS --> N2
```

## The Scaling Problem

When `Notification.Worker` scales horizontally, a client may be connected to Instance #1, but an `OrderCompletedEvent` arrives at Instance #2. Without a backplane, Instance #2 cannot deliver the message.

## Solution: Redis Backplane

All instances connect to a shared Redis cluster. When any instance sends a message to a user, Redis broadcasts it to all instances. The instance holding the WebSocket connection performs the final delivery.

```csharp
// Notification.Worker — Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("redis")!,
        options => { options.Configuration.ChannelPrefix = RedisChannel.Literal("marketplace"); });
```

## YARP Sticky Sessions

SignalR negotiation (HTTP) and upgrade (WebSocket) must hit the same backend instance:

```json
{
  "notificationCluster": {
    "SessionAffinity": {
      "Enabled": true,
      "Policy": "HashCookie",
      "FailurePolicy": "Redistribute",
      "AffinityKeyName": "SignalR_Affinity"
    }
  }
}
```

In Azure Container Apps, also enable: `affinity: "sticky"` on the Notification.Worker Ingress.

## Event Flow Example

1. `Payment.API` publishes `PaymentCompletedEvent` to bus
2. `Notification.Worker` (any instance) consumes the event
3. Worker calls `hubContext.Clients.User(buyerId).SendAsync("OrderUpdate", payload)`
4. Redis backplane routes to the instance holding the user's WebSocket
5. Client receives real-time push notification
