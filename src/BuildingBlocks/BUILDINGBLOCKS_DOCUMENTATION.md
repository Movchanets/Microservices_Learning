# BuildingBlocks — Shared Documentation

> **Generated**: 2026-05-21
> **Scope**: All types in `src/BuildingBlocks/` (SharedContracts + Infrastructure)

---

## Project Structure

```
src/BuildingBlocks/
├── SharedContracts/                         # Zero-dependency contracts (only MediatR.Contracts)
│   ├── Abstractions/                        # DDD base types
│   │   ├── AggregateRoot.cs
│   │   ├── Entity.cs
│   │   ├── IDomainEvent.cs
│   │   ├── IRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   └── ValueObject.cs
│   ├── Commands/                            # Integration commands (saga → service)
│   │   ├── Inventory/
│   │   │   ├── CancelReservationCommand.cs
│   │   │   └── ReserveInventoryCommand.cs
│   │   └── Payment/
│   │       ├── ProcessPaymentCommand.cs
│   │       └── RefundPaymentIntegrationCommand.cs
│   ├── Dtos/                                # Shared DTOs for cross-service payloads
│   │   └── OrderItemContract.cs
│   └── Events/                              # Integration events (service → service)
│       ├── Cart/
│       │   └── OrderSubmittedEvent.cs
│       ├── Catalog/
│       │   ├── ProductCreatedEvent.cs
│       │   ├── ProductDeletedEvent.cs
│       │   ├── ProductPriceChangedEvent.cs
│       │   └── ProductUpdatedEvent.cs
│       ├── Identity/
│       │   └── UserRegisteredIntegrationEvent.cs
│       ├── Inventory/
│       │   ├── InventoryReleasedEvent.cs
│       │   ├── InventoryReservationFailedEvent.cs
│       │   └── InventoryReservedEvent.cs
│       ├── Ordering/
│       │   ├── CancelOrderEvent.cs
│       │   ├── OrderCancelledEvent.cs
│       │   ├── OrderCompletedEvent.cs
│       │   └── OrderStatusChangedEvent.cs
│       ├── Payment/
│       │   ├── PaymentCompletedEvent.cs
│       │   ├── PaymentFailedEvent.cs
│       │   └── PaymentRefundedEvent.cs
│       └── StoreManagement/
│           └── StoreVerifiedIntegrationEvent.cs
│
└── Infrastructure/                          # Cross-cutting concerns (references SharedContracts)
    ├── Behaviors/
    │   ├── LoggingBehavior.cs
    │   └── ValidationBehavior.cs
    ├── Middleware/
    │   └── GlobalExceptionMiddleware.cs
    └── Models/
        ├── PagedResult.cs
        └── Result.cs
```

### Dependency Rules

| Project | References | NuGet Packages |
|:---|:---|:---|
| **SharedContracts** | *(none)* | `MediatR.Contracts 2.0.1` |
| **Infrastructure** | SharedContracts | `MediatR 14.1.0`, `FluentValidation 12.1.1`, `Serilog.AspNetCore 10.0.0` |

> **SharedContracts** has ZERO infrastructure dependencies — safe to reference from any layer (Domain, Application, Infrastructure, API).
> **Infrastructure** is the only project that pulls in EF Core-adjacent and DI packages.

---

## Part 1: SharedContracts — Abstractions

### 1.1 Entity

**File**: `SharedContracts/Abstractions/Entity.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Abstractions`

Base class for all domain entities with identity semantics.

```csharp
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public override bool Equals(object? obj) => obj is Entity other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}
```

| Member | Type | Description |
|:---|:---|:---|
| `Id` | `Guid` | Unique identity. Auto-generated on construction; settable only via `init`. |
| `Equals` | override | Identity-based equality (compares `Id` only, not attributes). |
| `GetHashCode` | override | Hash based on `Id` — consistent with `Equals` for use in `HashSet`/`Dictionary`. |

**Usage**: Every entity in every microservice inherits from `Entity`. Equality is always identity-based.

---

### 1.2 AggregateRoot

**File**: `SharedContracts/Abstractions/AggregateRoot.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Abstractions`

Base class for DDD aggregate roots. Extends `Entity` with domain event collection.

```csharp
public abstract class AggregateRoot : Entity
{
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }
    protected void AddDomainEvent(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}
```

| Member | Type | Description |
|:---|:---|:---|
| `DomainEvents` | `IReadOnlyList<IDomainEvent>` | Read-only view of collected domain events. |
| `AddDomainEvent` | protected method | Records a domain event within the aggregate. |
| `ClearDomainEvents` | public method | Clears all events — call after dispatching via MediatR or outbox. |

**Lifecycle**:
1. Aggregate mutates state → calls `AddDomainEvent(...)`.
2. After `SaveChangesAsync`, the outbox/dispatcher reads `DomainEvents`.
3. Dispatcher calls `ClearDomainEvents()` to prevent re-publishing.

**Performance note**: The `ReadOnlyCollection` wrapper is allocated once in the constructor, not on every property access.

---

### 1.3 IDomainEvent

**File**: `SharedContracts/Abstractions/IDomainEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Abstractions`

Marker interface for intra-service domain events. Derives from MediatR's `INotification`.

```csharp
public interface IDomainEvent : INotification;
```

**Usage**: Implement on records/classes that represent domain events dispatched within a single bounded context via MediatR `Publish`.

---

### 1.4 IRepository\<T\>

**File**: `SharedContracts/Abstractions/IRepository.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Abstractions`

Generic repository contract, constrained to `AggregateRoot`.

```csharp
public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

| Method | Returns | Description |
|:---|:---|:---|
| `GetByIdAsync` | `Task<T?>` | Load aggregate by ID, null if not found. |
| `Add` | `void` | Track a new aggregate for insertion. |
| `Update` | `void` | Mark an existing aggregate as modified. |
| `Remove` | `void` | Mark an aggregate for deletion. |

**Implementation**: Each microservice implements this in its Infrastructure layer using EF Core.

---

### 1.5 IUnitOfWork

**File**: `SharedContracts/Abstractions/IUnitOfWork.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Abstractions`

Transaction boundary abstraction.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**Implementation**: Each microservice's `DbContext` implements `IUnitOfWork`, wrapping `DbContext.SaveChangesAsync`.

---

### 1.6 ValueObject

**File**: `SharedContracts/Abstractions/ValueObject.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Abstractions`

Base class for value objects — equality by component values, not identity.

```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();
    public bool Equals(ValueObject? other);
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(ValueObject? left, ValueObject? right);
    public static bool operator !=(ValueObject? left, ValueObject? right);
}
```

**Usage**: Override `GetEqualityComponents()` to yield the properties that define equivalence.

```csharp
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}
```

---

## Part 2: SharedContracts — Integration Commands

Integration commands are sent from a **saga orchestrator** to a specific service via MassTransit.

### 2.1 ReserveInventoryCommand

**File**: `SharedContracts/Commands/Inventory/ReserveInventoryCommand.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Commands.Inventory`

```csharp
public record ReserveInventoryCommand(
    Guid CorrelationId,   // Saga correlation ID
    Guid OrderId,         // Order being fulfilled
    List<OrderItemContract> Items);  // Products to reserve
```

**Flow**: Ordering Saga → Inventory Service (via RabbitMQ/Azure Service Bus)

---

### 2.2 CancelReservationCommand

**File**: `SharedContracts/Commands/Inventory/CancelReservationCommand.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Commands.Inventory`

```csharp
public record CancelReservationCommand(
    Guid CorrelationId,
    Guid OrderId,
    List<OrderItemContract> Items);
```

**Flow**: Ordering Saga → Inventory Service (compensation/rollback)

---

### 2.3 ProcessPaymentCommand

**File**: `SharedContracts/Commands/Payment/ProcessPaymentCommand.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Commands.Payment`

```csharp
public record ProcessPaymentCommand(
    Guid CorrelationId,
    Guid OrderId,
    decimal Amount,
    string BuyerId);
```

**Flow**: Ordering Saga → Payment Service

---

### 2.4 RefundPaymentIntegrationCommand

**File**: `SharedContracts/Commands/Payment/RefundPaymentIntegrationCommand.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Commands.Payment`

```csharp
public record RefundPaymentIntegrationCommand(
    Guid CorrelationId,
    Guid OrderId,
    Guid TransactionId,   // Original payment transaction
    decimal Amount,
    string Reason);
```

**Flow**: Ordering Saga → Payment Service (compensation/rollback)

---

## Part 3: SharedContracts — Shared DTOs

### 3.1 OrderItemContract

**File**: `SharedContracts/Dtos/OrderItemContract.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Dtos`

Shared data contract for order line items used across multiple integration events and commands.

```csharp
public record OrderItemContract(
    Guid ProductId,
    int Quantity,
    decimal Price,
    Guid StoreId);
```

| Property | Type | Description |
|:---|:---|:---|
| `ProductId` | `Guid` | Reference to the catalog product. |
| `Quantity` | `int` | Number of units ordered. |
| `Price` | `decimal` | Unit price at time of order (snapshot). |
| `StoreId` | `Guid` | The store selling this product. |

**Used by**: `OrderSubmittedEvent`, `ReserveInventoryCommand`, `CancelReservationCommand`.

---

## Part 4: SharedContracts — Integration Events

Integration events are published by one microservice and consumed by one or more others via MassTransit.

### 4.1 Cart Domain

#### OrderSubmittedEvent

**File**: `SharedContracts/Events/Cart/OrderSubmittedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Cart`

Published when a buyer completes checkout from the Cart service.

```csharp
public record OrderSubmittedEvent(
    Guid CorrelationId,
    string BuyerId,
    List<OrderItemContract> Items,
    DateTime Timestamp,
    string? ShippingAddressLine1 = null,
    string? ShippingAddressLine2 = null,
    string? ShippingCity = null,
    string? ShippingState = null,
    string? ShippingPostalCode = null,
    string? ShippingCountry = null);
```

| Property | Type | Description |
|:---|:---|:---|
| `CorrelationId` | `Guid` | Saga correlation ID — traces the entire order flow. |
| `BuyerId` | `string` | Identity of the buyer (from Identity service). |
| `Items` | `List<OrderItemContract>` | Cart items at time of submission. |
| `Timestamp` | `DateTime` | When the order was submitted. |
| `ShippingAddress*` | `string?` | Optional shipping address fields. |

**Consumers**: Ordering Service (creates the order aggregate and starts the saga).

---

### 4.2 Catalog Domain

#### ProductCreatedEvent

**File**: `SharedContracts/Events/Catalog/ProductCreatedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Catalog`

```csharp
public sealed record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    string CategoryName,
    List<string> Tags,
    string? ImageUrl,
    Guid StoreId,
    DateTime CreatedAt,
    string? Brand = null,
    Dictionary<string, string>? Attributes = null);
```

**Publisher**: Catalog.API
**Consumer**: Search.API (indexes product in Elasticsearch)

#### ProductUpdatedEvent

**File**: `SharedContracts/Events/Catalog/ProductUpdatedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Catalog`

```csharp
public sealed record ProductUpdatedEvent(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    string CategoryName,
    List<string> Tags,
    string? ImageUrl,
    Guid StoreId,
    bool IsActive,
    DateTime UpdatedAt,
    string? Brand = null,
    Dictionary<string, string>? Attributes = null);
```

**Publisher**: Catalog.API
**Consumer**: Search.API (re-indexes product)

#### ProductDeletedEvent

**File**: `SharedContracts/Events/Catalog/ProductDeletedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Catalog`

```csharp
public sealed record ProductDeletedEvent(
    Guid ProductId,
    DateTime DeletedAt);
```

**Publisher**: Catalog.API (soft-delete)
**Consumer**: Search.API (removes from Elasticsearch index)

#### ProductPriceChangedEvent

**File**: `SharedContracts/Events/Catalog/ProductPriceChangedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Catalog`

```csharp
public sealed record ProductPriceChangedEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency,
    DateTime ChangedAt);
```

**Publisher**: Catalog.API
**Consumers**: Inventory/Cart (price validation on active carts)

---

### 4.3 Identity Domain

#### UserRegisteredIntegrationEvent

**File**: `SharedContracts/Events/Identity/UserRegisteredIntegrationEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Identity`

```csharp
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime Timestamp);
```

**Publisher**: Identity.API
**Consumers**: StoreManagement (auto-creates seller store on seller registration), Notification (welcome email)

---

### 4.4 Inventory Domain

#### InventoryReservedEvent

**File**: `SharedContracts/Events/Inventory/InventoryReservedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Inventory`

```csharp
public record InventoryReservedEvent(
    Guid CorrelationId,
    Guid OrderId);
```

**Publisher**: Inventory Service (after successful reservation)
**Consumer**: Ordering Saga (advances to payment step)

#### InventoryReservationFailedEvent

**File**: `SharedContracts/Events/Inventory/InventoryReservationFailedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Inventory`

```csharp
public record InventoryReservationFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string Reason);
```

**Publisher**: Inventory Service (insufficient stock, product not found, etc.)
**Consumer**: Ordering Saga (triggers order cancellation / compensation)

#### InventoryReleasedEvent

**File**: `SharedContracts/Events/Inventory/InventoryReleasedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Inventory`

```csharp
public record InventoryReleasedEvent(
    Guid CorrelationId,
    Guid OrderId);
```

**Publisher**: Inventory Service (after cancellation or rollback)
**Consumer**: Ordering Saga (confirms compensation completed)

---

### 4.5 Ordering Domain

#### OrderStatusChangedEvent

**File**: `SharedContracts/Events/Ordering/OrderStatusChangedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Ordering`

```csharp
public record OrderStatusChangedEvent(
    Guid OrderId,
    string BuyerId,
    string NewStatus,
    string? Notes,
    DateTime Timestamp);
```

**Publisher**: Ordering Service
**Consumers**: Notification (push notification to buyer), StoreManagement (seller dashboard updates)

#### OrderCompletedEvent

**File**: `SharedContracts/Events/Ordering/OrderCompletedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Ordering`

```csharp
public record OrderCompletedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId);
```

**Publisher**: Ordering Saga (final state — all steps succeeded)
**Consumers**: Notification (order confirmation), Analytics

#### CancelOrderEvent

**File**: `SharedContracts/Events/Ordering/CancelOrderEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Ordering`

```csharp
public record CancelOrderEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId,
    string? Reason,
    DateTime Timestamp);
```

**Publisher**: Ordering Saga or API (buyer requests cancellation)
**Consumers**: Inventory (release stock), Payment (refund)

#### OrderCancelledEvent

**File**: `SharedContracts/Events/Ordering/OrderCancelledEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Ordering`

```csharp
public record OrderCancelledEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId,
    string Reason,
    DateTime Timestamp = default);
```

**Publisher**: Ordering Saga (after all compensations complete)
**Consumers**: Notification (cancellation confirmation to buyer)

---

### 4.6 Payment Domain

#### PaymentCompletedEvent

**File**: `SharedContracts/Events/Payment/PaymentCompletedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Payment`

```csharp
public record PaymentCompletedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string TransactionId);
```

**Publisher**: Payment Service (successful charge)
**Consumer**: Ordering Saga (advances to order completion)

#### PaymentFailedEvent

**File**: `SharedContracts/Events/Payment/PaymentFailedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Payment`

```csharp
public record PaymentFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string FailureReason);
```

**Publisher**: Payment Service (card declined, gateway error, etc.)
**Consumer**: Ordering Saga (triggers full compensation: release inventory, cancel order)

#### PaymentRefundedEvent

**File**: `SharedContracts/Events/Payment/PaymentRefundedEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.Payment`

```csharp
public record PaymentRefundedEvent(
    Guid CorrelationId,
    Guid OrderId,
    Guid TransactionId,
    Guid RefundId,
    decimal Amount,
    string Reason);
```

**Publisher**: Payment Service (refund processed)
**Consumer**: Ordering Saga (confirms compensation completed)

---

### 4.7 StoreManagement Domain

#### StoreVerifiedIntegrationEvent

**File**: `SharedContracts/Events/StoreManagement/StoreVerifiedIntegrationEvent.cs`
**Namespace**: `BuildingBlocks.SharedContracts.Events.StoreManagement`

```csharp
public sealed record StoreVerifiedIntegrationEvent(
    Guid StoreId,
    string SellerId,
    DateTime Timestamp);
```

**Publisher**: StoreManagement (admin verifies a seller's store)
**Consumers**: Catalog (enables product creation for that store), Notification (seller notification)

---

## Part 5: Infrastructure — Pipeline Behaviors

### 5.1 ValidationBehavior\<TRequest, TResponse\>

**File**: `Infrastructure/Behaviors/ValidationBehavior.cs`
**Namespace**: `BuildingBlocks.Infrastructure.Behaviors`

MediatR pipeline behavior that runs **all** FluentValidation validators before the handler executes. Throws `FluentValidation.ValidationException` if any rules fail.

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
```

**Pipeline order**: Validation → Logging → Handler

**Registration** (every microservice `Program.cs`):
```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(SomeValidator).Assembly);
```

**Design rationale**: Fail-Fast principle — handlers never receive invalid data, eliminating redundant validation checks in business logic.

---

### 5.2 LoggingBehavior\<TRequest, TResponse\>

**File**: `Infrastructure/Behaviors/LoggingBehavior.cs`
**Namespace**: `BuildingBlocks.Infrastructure.Behaviors`

MediatR pipeline behavior that logs request start/end with elapsed time. Emits a `[SLOW]` warning if the handler takes >500ms.

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
```

**Log output**:
```
[START] CreateOrderCommand
[SLOW] CreateOrderCommand took 1234ms    ← only if >500ms
[END] CreateOrderCommand (1234ms)
```

**Registration**:
```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

---

## Part 6: Infrastructure — Middleware

### 6.1 GlobalExceptionMiddleware

**File**: `Infrastructure/Middleware/GlobalExceptionMiddleware.cs`
**Namespace**: `BuildingBlocks.Infrastructure.Middleware`

Catches unhandled exceptions and returns RFC 7807 `ProblemDetails` JSON responses.

```csharp
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment env)
```

**Exception → HTTP Status mapping**:

| Exception Type | HTTP Status | Title |
|:---|:---|:---|
| `OperationCanceledException` | 503 | Request Cancelled |
| `ArgumentException` | 400 | Bad Request |
| `KeyNotFoundException` | 404 | Not Found |
| `UnauthorizedAccessException` | 401 | Unauthorized |
| `InvalidOperationException` | 409 | Conflict |
| `DbUpdateConcurrencyException` | 409 | Data Conflict |
| `DbUpdateException` | 409 | Data Conflict |
| *(any other)* | 500 | Internal Server Error |

> EF Core exceptions are detected by **type name** (string comparison) to avoid adding an EF Core dependency to BuildingBlocks.

**Registration** (must be first in pipeline):
```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();
```

**Response format**:
```json
{
  "type": null,
  "title": "Not Found",
  "status": 404,
  "detail": "Product with ID xyz not found.",
  "instance": "/api/products/xyz"
}
```

In production, generic exceptions return `"An unexpected error occurred."` instead of the actual message.

---

## Part 7: Infrastructure — Models

### 7.1 Result\<T\>

**File**: `Infrastructure/Models/Result.cs`
**Namespace**: `BuildingBlocks.Infrastructure.Models`

Generic result type implementing the **Result pattern** — avoids throwing exceptions for expected/anticipated failures.

```csharp
public sealed class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }
    public bool IsSuccess { get; }

    public static Result<T> Success(T value);
    public static Result<T> Failure(string error, string errorCode = "ERROR");
}
```

| Factory | Returns | Example |
|:---|:---|:---|
| `Result<T>.Success(value)` | Success result | `Result<Guid>.Success(orderId)` |
| `Result<T>.Failure(msg, code)` | Failure result | `Result<Guid>.Failure("Out of stock", "INSUFFICIENT_STOCK")` |

**Usage in handlers**:
```csharp
public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
{
    if (stock < cmd.Quantity)
        return Result<Guid>.Failure("Insufficient stock", "INSUFFICIENT_STOCK");

    // ... create order
    return Result<Guid>.Success(order.Id);
}
```

**Usage in endpoints**:
```csharp
var result = await sender.Send(cmd, ct);
return result.IsSuccess
    ? Results.Created($"/api/orders/{result.Value}", result.Value)
    : Results.BadRequest(new { result.Error, result.ErrorCode });
```

---

### 7.2 PagedResult\<T\>

**File**: `Infrastructure/Models/PagedResult.cs`
**Namespace**: `BuildingBlocks.Infrastructure.Models`

Standard paginated response wrapper for all list/query endpoints.

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages { get; }  // computed: ceiling(TotalCount / PageSize)
    public bool HasPrevious { get; } // Page > 1
    public bool HasNext { get; }     // Page < TotalPages
}
```

| Property | Type | Description |
|:---|:---|:---|
| `Items` | `IReadOnlyList<T>` | The current page of results. |
| `TotalCount` | `int` | Total items across all pages. |
| `Page` | `int` | Current page number (1-based). |
| `PageSize` | `int` | Items per page. |
| `TotalPages` | `int` | Computed total pages. |
| `HasPrevious` | `bool` | Whether a previous page exists. |
| `HasNext` | `bool` | Whether a next page exists. |

**Usage**:
```csharp
var products = await _context.Products
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

return new PagedResult<ProductDto>(products, totalCount, page, pageSize);
```

---

## Event Flow Diagram

```
┌──────────┐  OrderSubmittedEvent   ┌──────────────┐
│   Cart   │ ─────────────────────► │   Ordering   │
│ Service  │                        │   Service    │
└──────────┘                        │   (Saga)     │
                                    └──────┬───────┘
                                           │
              ┌────────────────────────────┼────────────────────────┐
              │                            │                        │
              ▼                            ▼                        ▼
    ReserveInventoryCmd          ProcessPaymentCmd         OrderStatusChanged
              │                            │                        │
              ▼                            ▼                        ▼
    ┌─────────────────┐          ┌─────────────────┐      ┌────────────────┐
    │   Inventory     │          │    Payment      │      │  Notification  │
    │   Service       │          │    Service      │      │  Service       │
    └────────┬────────┘          └────────┬────────┘      └────────────────┘
             │                            │
             ▼                            ▼
    InventoryReservedEvent       PaymentCompletedEvent
             │                            │
             └────────────┬───────────────┘
                          ▼
                  OrderCompletedEvent
                          │
                          ▼
                  Notification (email/push)
```

---

## Quick Reference: All Types by Namespace

| Namespace | Type | Kind | Description |
|:---|:---|:---|:---|
| `Abstractions` | `Entity` | abstract class | Identity-based entity base |
| `Abstractions` | `AggregateRoot` | abstract class | Entity + domain events |
| `Abstractions` | `IDomainEvent` | interface | MediatR notification marker |
| `Abstractions` | `IRepository<T>` | interface | Generic repository contract |
| `Abstractions` | `IUnitOfWork` | interface | Transaction boundary |
| `Abstractions` | `ValueObject` | abstract class | Value-based equality |
| `Commands.Inventory` | `ReserveInventoryCommand` | record | Saga → Inventory |
| `Commands.Inventory` | `CancelReservationCommand` | record | Saga → Inventory (compensation) |
| `Commands.Payment` | `ProcessPaymentCommand` | record | Saga → Payment |
| `Commands.Payment` | `RefundPaymentIntegrationCommand` | record | Saga → Payment (compensation) |
| `Dtos` | `OrderItemContract` | record | Shared order line item |
| `Events.Cart` | `OrderSubmittedEvent` | record | Cart → Ordering |
| `Events.Catalog` | `ProductCreatedEvent` | sealed record | Catalog → Search |
| `Events.Catalog` | `ProductUpdatedEvent` | sealed record | Catalog → Search |
| `Events.Catalog` | `ProductDeletedEvent` | sealed record | Catalog → Search |
| `Events.Catalog` | `ProductPriceChangedEvent` | sealed record | Catalog → Inventory/Cart |
| `Events.Identity` | `UserRegisteredIntegrationEvent` | sealed record | Identity → Store/Notification |
| `Events.Inventory` | `InventoryReservedEvent` | record | Inventory → Ordering Saga |
| `Events.Inventory` | `InventoryReservationFailedEvent` | record | Inventory → Ordering Saga |
| `Events.Inventory` | `InventoryReleasedEvent` | record | Inventory → Ordering Saga |
| `Events.Ordering` | `OrderStatusChangedEvent` | record | Ordering → Notification/Store |
| `Events.Ordering` | `OrderCompletedEvent` | record | Ordering Saga → Notification |
| `Events.Ordering` | `CancelOrderEvent` | record | Ordering → Inventory/Payment |
| `Events.Ordering` | `OrderCancelledEvent` | record | Ordering Saga → Notification |
| `Events.Payment` | `PaymentCompletedEvent` | record | Payment → Ordering Saga |
| `Events.Payment` | `PaymentFailedEvent` | record | Payment → Ordering Saga |
| `Events.Payment` | `PaymentRefundedEvent` | record | Payment → Ordering Saga |
| `Events.StoreManagement` | `StoreVerifiedIntegrationEvent` | sealed record | Store → Catalog/Notification |
| `Infrastructure.Behaviors` | `ValidationBehavior<T,R>` | class | FluentValidation pipeline |
| `Infrastructure.Behaviors` | `LoggingBehavior<T,R>` | class | Request logging pipeline |
| `Infrastructure.Middleware` | `GlobalExceptionMiddleware` | class | RFC 7807 error handler |
| `Infrastructure.Models` | `Result<T>` | class | Success/failure result |
| `Infrastructure.Models` | `PagedResult<T>` | record | Paginated response |
