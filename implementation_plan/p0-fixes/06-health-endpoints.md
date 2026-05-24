# P0-06 — Health Endpoints Fix

**Goal**: Uncomment all services in the gateway health endpoint so monitoring covers the full system.

**Fixes**: MISSING.md #7.1

---

## Step 1: Uncomment services in HealthEndpoints

File: `src/Gateways/ApiGateway/Endpoints/HealthEndpoints.cs`

Change:
```csharp
private static readonly string[] ServiceNames =
[
    "identity-api",
    //uncomment when implemented
    //"catalog-api",
    //"ordering-api",
    //"inventory-api",
    //"cart-api",
    //"search-api",
    //"store-api",
    //"media-api",
    //"payment-api",
    //"notification-worker"
];
```

To:
```csharp
private static readonly string[] ServiceNames =
[
    "identity-api",
    "catalog-api",
    "ordering-api",
    "inventory-api",
    "cart-api",
    "search-api",
    "store-api",
    "media-api",
    "payment-api"
];
```

Note: `notification-worker` is excluded because it's a worker service without HTTP endpoints (per team memory: "Scalar excludes workers").

## Step 2: Verify HttpClients are registered

File: `src/Gateways/ApiGateway/Program.cs`

Ensure all services have named HttpClients registered:
```csharp
builder.Services.AddHttpClient("identity-api", ...);
builder.Services.AddHttpClient("catalog-api", ...);
// etc.
```

If they're not registered, add them using Aspire service discovery.

## Verification
- `dotnet build Marketplace.slnx`
- Call `GET /bff/health` — all services should report Healthy
- Call `GET /bff/health/catalog-api` — individual service health

## Done When
- [ ] All services uncommented in ServiceNames array
- [ ] HttpClients registered for each service
- [ ] /bff/health returns all services
