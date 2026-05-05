# Phase 0 — Foundation & .NET Aspire Scaffolding

**Goal**: Set up the monorepo skeleton, Aspire orchestration, shared BuildingBlocks, and infrastructure containers.

## Prerequisites
- .NET 10 SDK installed
- Docker Desktop running
- Node.js 20+ for Angular

## Tasks

- [ ] **Create solution file** `Marketplace.sln` at repository root
- [ ] **Scaffold `Marketplace.AppHost`** project in `src/Aspire/Marketplace.AppHost/`
  - Add PostgreSQL resource with per-service databases (identity-db, catalog-db, ordering-db, inventory-db, payment-db, store-db)
  - Add Redis resource
  - Add RabbitMQ resource
  - Add Elasticsearch container (custom hosting integration)
- [ ] **Scaffold `Marketplace.ServiceDefaults`** in `src/Aspire/Marketplace.ServiceDefaults/`
  - Configure OpenTelemetry (tracing, metrics, logging)
  - Add default health checks (liveness + readiness)
  - Configure `HttpClient` resilience (retries, circuit breaker) via `AddStandardResilienceHandler()`
  - Configure service discovery
- [ ] **Create `BuildingBlocks.SharedContracts`** class library in `src/BuildingBlocks/SharedContracts/`
  - Define base types: `AggregateRoot`, `Entity`, `ValueObject`, `IDomainEvent`
  - Define `IRepository<T>`, `IUnitOfWork` interfaces
  - Create `Events/` folder for integration event contracts (empty for now)
  - Create `Commands/` folder for command contracts (empty for now)
- [ ] **Create `BuildingBlocks.Infrastructure`** class library in `src/BuildingBlocks/Infrastructure/`
  - Global exception handling middleware → `ProblemDetails` response
  - `PagedResult<T>` response wrapper
  - MediatR pipeline behaviors: `ValidationBehavior<TRequest, TResponse>`, `LoggingBehavior<TRequest, TResponse>`
  - Serilog configuration extension methods
- [ ] **Add all projects to `Marketplace.sln`**
- [ ] **Verify** `dotnet build Marketplace.sln` compiles without errors
- [ ] **Verify** `dotnet run --project src/Aspire/Marketplace.AppHost` launches Aspire dashboard with all infrastructure containers

## Deliverables
```
src/
├── Aspire/
│   ├── Marketplace.AppHost/
│   └── Marketplace.ServiceDefaults/
└── BuildingBlocks/
    ├── SharedContracts/
    └── Infrastructure/
```
