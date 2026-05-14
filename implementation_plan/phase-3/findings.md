# Phase 3 — Findings

## Existing Infrastructure (from AppHost.cs)
- `inventoryDb` — PostgreSQL database already declared
- `redis` — Redis resource already declared (with RedisInsight)
- `messaging` — RabbitMQ already declared
- YARP routes for `/api/inventory/**` and `/api/cart/**` already in gateway `appsettings.json`
- Phase 3 placeholder comments in AppHost.cs at lines 78-79

## SharedContracts Structure
- `Events/Catalog/` — 4 event files exist (pattern to follow)
- `Commands/` — empty (`.gitkeep`)
- Need to create: `Commands/Inventory/`, `Events/Inventory/`, `Events/Cart/`
- Need shared type: `OrderItemContract`

## BuildingBlocks.Infrastructure
- `Result<T>` — generic result type
- `PagedResult<T>` — pagination wrapper
- `ValidationBehavior<,>` — MediatR pipeline behavior
- `LoggingBehavior<,>` — MediatR pipeline behavior
- `GlobalExceptionMiddleware` — exception handling middleware

## BuildingBlocks.SharedContracts.Abstractions
- `AggregateRoot` — base class with `Id`, `DomainEvents`
- `Entity` — base class with `Id`
- `ValueObject` — abstract base with `GetEqualityComponents()`
- `IRepository<T>` — `GetByIdAsync`, `Add`, `Update`, `Remove`
- `IUnitOfWork` — `SaveChangesAsync`
- `IDomainEvent` — marker interface for MediatR `INotification`
