# 04 — Monorepo Structure & BuildingBlocks

## Repository Layout

```
d:\code\Microservices\
├── plans/                            # Architecture documentation (this folder)
├── src/
│   ├── Aspire/
│   │   ├── Marketplace.AppHost/      # Orchestration: resources, dependencies, manifests
│   │   └── Marketplace.ServiceDefaults/ # Shared: OpenTelemetry, Health Checks, Resilience
│   │
│   ├── Gateways/
│   │   └── ApiGateway/               # YARP reverse proxy + BFF logic
│   │
│   ├── Microservices/
│   │   ├── Identity/                 # Identity.API (auth, users, roles)
│   │   ├── Catalog/                  # Catalog.API (products, categories)
│   │   ├── Search/                   # Search.API (Elasticsearch)
│   │   ├── Inventory/                # Inventory.API (stock, reservations)
│   │   ├── Cart/                     # Cart.API (Redis sessions)
│   │   ├── Ordering/                 # Ordering.API (Saga state machine)
│   │   ├── Payment/                  # Payment.API (external gateways)
│   │   ├── StoreManagement/          # StoreManagement.API (sellers)
│   │   ├── Media/                    # Media.API (Blob storage)
│   │   └── Notification/             # Notification.Worker (SignalR push)
│   │
│   ├── BuildingBlocks/
│   │   ├── SharedContracts/          # Integration event interfaces/records
│   │   └── Infrastructure/           # Cross-cutting: exception handling, Swagger, base configs
│   │
│   └── web/                          # Angular 19+ SPA
│
├── tests/
│   ├── UnitTests/                    # xUnit + Moq + FluentAssertions
│   ├── IntegrationTests/             # Testcontainers (PostgreSQL, RabbitMQ, Redis)
│   └── E2ETests/                     # Playwright
│
├── AGENTS.md                         # AI agent instructions
└── Marketplace.sln                   # Solution file
```

## BuildingBlocks Rules

### ✅ ALLOWED in BuildingBlocks

1. **Integration event contracts** — Flat interfaces/records for MassTransit messaging
   ```csharp
   // SharedContracts/Events/OrderSubmittedEvent.cs
   public record OrderSubmittedEvent(
       Guid CorrelationId,
       string BuyerId,
       List<OrderItemContract> Items,
       DateTime Timestamp);
   ```

2. **Base abstractions** — `IRepository<T>`, `IUnitOfWork`, `IDomainEvent`, `AggregateRoot`

3. **Cross-cutting infrastructure** — Global exception middleware, `ProblemDetails` wrappers, Serilog/OpenTelemetry extensions, `PagedResult<T>`

4. **Versioned contracts** — When evolving: create `OrderSubmittedEventV2`, consumers migrate gradually

### ❌ FORBIDDEN in BuildingBlocks

1. **Shared domain entities** — Each service models its own context (`Product` in Catalog ≠ `InventoryItem` in Inventory)

2. **ORM dependencies** — No `Microsoft.EntityFrameworkCore.*` or `Npgsql.*` references

3. **Business logic** — No discount calculations, permission checks, or domain rules

> BuildingBlocks = **infrastructure glue + contracts**, NOT a central business library.

## Aspire AppHost Configuration

```csharp
// src/Aspire/Marketplace.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var rabbit = builder.AddRabbitMQ("messaging");

// Databases (per service)
var catalogDb = postgres.AddDatabase("catalog-db");
var orderingDb = postgres.AddDatabase("ordering-db");
var inventoryDb = postgres.AddDatabase("inventory-db");
var identityDb = postgres.AddDatabase("identity-db");
var paymentDb = postgres.AddDatabase("payment-db");
var storeDb = postgres.AddDatabase("store-db");

// Microservices
var identity = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb)
    .WithReference(rabbit);

var catalog = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(rabbit);

var ordering = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(orderingDb)
    .WithReference(rabbit);

// ... similar for other services

// Gateway
var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(identity)
    .WithReference(catalog)
    .WithReference(ordering)
    .WithReference(redis);

// Angular frontend
builder.AddNpmApp("angular", "../web")
    .WithReference(gateway)
    .WithHttpEndpoint(port: 4200)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```
