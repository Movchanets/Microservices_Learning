# Monorepo Structure & Conventions

> **Last Updated:** 2026-05-26
> **Source:** `plans/04-monorepo-structure.md`

---

## Repository Layout

```
d:\code\Microservices\
├── plans/                            # Architecture documentation
│   ├── product-sku-refactor/         # SKU refactor task plan & phases
│   ├── next_steps/                   # Future feature plans
│   ├── future_design/                # UI/UX design plans
│   └── *.md                          # Numbered plan docs (01-13)
│
├── docs/                             # Living documentation
│   ├── architecture/                 # System architecture docs
│   └── status/                       # Project status tracking
│
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
│   │   │   ├── Identity.API/         # Presentation layer (endpoints, Program.cs)
│   │   │   ├── Identity.Application/ # Commands, queries, DTOs, validators
│   │   │   ├── Identity.Domain/      # Aggregates, entities, value objects, domain events
│   │   │   └── Identity.Infrastructure/ # EF Core, repositories, consumers, event handlers
│   │   │
│   │   ├── Catalog/                  # Catalog.API (products, categories, SKUs)
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
│   │   ├── SharedContracts/          # Integration event interfaces/records, DTOs, abstractions
│   │   │   ├── Abstractions/         # AggregateRoot, Entity, IDomainEvent
│   │   │   ├── Dtos/                 # OrderItemContract, etc.
│   │   │   └── Events/               # Integration events by service
│   │   │       ├── Catalog/
│   │   │       ├── Inventory/
│   │   │       ├── Ordering/
│   │   │       ├── Payment/
│   │   │       ├── StoreManagement/
│   │   │       └── Identity/
│   │   └── Infrastructure/           # Cross-cutting: exception handling, Swagger, base configs
│   │       ├── Database/             # DomainEventsDbContext, interceptors
│   │       └── Messaging/            # MassTransit base configurations
│   │
│   └── web/                          # Angular 19+ SPA
│       └── src/app/
│           ├── features/             # Feature modules (catalog, cart, seller-dashboard)
│           ├── shared/               # Shared components, services, guards
│           └── core/                 # Core services (auth, API client)
│
├── tests/
│   ├── UnitTests/                    # xUnit + Moq + FluentAssertions
│   │   ├── Catalog.UnitTests/
│   │   ├── Inventory.UnitTests/
│   │   ├── Search.UnitTests/
│   │   └── ...
│   ├── IntegrationTests/             # Testcontainers (PostgreSQL, RabbitMQ, Redis)
│   ├── ContractTests/                # MassTransit contract verification
│   └── E2ETests/                     # Playwright
│
├── AGENTS.md                         # AI agent instructions
└── Marketplace.sln                   # Solution file
```

---

## BuildingBlocks Rules

### ✅ ALLOWED in BuildingBlocks

| What | Example |
|:---|:---|
| Integration event contracts | `OrderSubmittedEvent`, `ProductCreatedEvent` (flat records) |
| Base abstractions | `AggregateRoot`, `Entity`, `IRepository<T>`, `IUnitOfWork`, `IDomainEvent` |
| Cross-cutting infrastructure | Global exception middleware, `ProblemDetails` wrappers, Serilog/OTEL extensions, `PagedResult<T>` |
| Shared DTOs | `OrderItemContract` — flat data transfer objects |
| Versioned contracts | `OrderSubmittedEventV2` for gradual migration |

### ❌ FORBIDDEN in BuildingBlocks

| What | Why |
|:---|:---|
| Shared domain entities | Each service models its own context (`Product` in Catalog ≠ `InventoryItem` in Inventory) |
| ORM dependencies | No `Microsoft.EntityFrameworkCore.*` or `Npgsql.*` references |
| Business logic | No discount calculations, permission checks, or domain rules |

> **BuildingBlocks = infrastructure glue + contracts, NOT a central business library.**

---

## Microservice Internals (Clean Architecture)

Each microservice follows Clean Architecture with 4 layers:

```
ServiceName.API/                     # Presentation — Minimal APIs, Program.cs
ServiceName.Application/             # Application — Commands, Queries, Handlers, DTOs, Validators
ServiceName.Domain/                  # Domain — Aggregates, Entities, Value Objects, Domain Events
ServiceName.Infrastructure/          # Infrastructure — EF Core, Repositories, Consumers, Event Handlers
```

### Layer Dependencies

```
API → Application → Domain
API → Infrastructure → Domain
Infrastructure → Domain (implements interfaces)
Domain → (no dependencies — pure C#)
```

### Application Layer Patterns

- **MediatR** for CQRS command/query dispatching
- **Pipeline Behaviors:** Validation → Logging → Transaction
- **FluentValidation** for input validation

---

## Aspire AppHost Configuration

```csharp
// src/Aspire/Marketplace.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
var rabbit = builder.AddRabbitMQ("messaging");

// Databases (per service — database-per-service pattern)
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

---

## Technology Stack

| Layer | Technology |
|:---|:---|
| Runtime | .NET 10 |
| API Framework | ASP.NET Core 10, Minimal APIs |
| ORM | EF Core 10 |
| Database | PostgreSQL |
| Cache | Redis (StackExchange.Redis) |
| Message Broker | RabbitMQ (local), Azure Service Bus (prod) |
| Messaging Library | MassTransit + Automatonymous |
| CQRS | MediatR |
| Validation | FluentValidation |
| Search | Elasticsearch (Nest) |
| Object Storage | Azure Blob / Azurite |
| Realtime | SignalR + Redis Backplane |
| Identity | Microsoft Entra ID (OIDC) |
| Frontend | Angular 19, Spartan/UI, Tailwind CSS |
| State Management | NgRx SignalStore |
| API Gateway | YARP |
| Orchestration | .NET Aspire |
| Testing | xUnit, Moq, FluentAssertions, Testcontainers, Playwright |
