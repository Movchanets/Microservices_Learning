# Inventory Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core + Npgsql) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Concurrency** | Optimistic (xmin / Version byte array) |
| **Project Path** | `src/Microservices/Inventory/` |

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `InventoryItem` | Aggregate Root | Id (PK), SkuId (FK to Catalog SKU), ProductId, StoreId, SkuCode, AvailableQuantity, ReservedQuantity, IsDeactivated, Version (optimistic concurrency) |

### Domain Methods

| Method | Description |
|:---|:---|
| `Create(skuId, productId, skuCode, initialQuantity, storeId)` | Factory method — creates new inventory item |
| `AddStock(quantity)` | Increases AvailableQuantity. Throws if deactivated. No domain event. |
| `Reserve(quantity)` | Moves stock from Available → Reserved. Fires `StockReservedDomainEvent`. |
| `Release(quantity)` | Returns reserved stock to Available. Fires `StockReleasedDomainEvent`. |
| `Deactivate()` | Zeros AvailableQuantity, sets IsDeactivated. Idempotent. Called when SKU is deleted in Catalog. |

### Domain Events Raised

| Event | Trigger |
|:---|:---|
| `StockReservedDomainEvent` | `Reserve()` — moves stock from Available → Reserved |
| `StockReleasedDomainEvent` | `Release()` — returns reserved stock to Available |

**Note:** `AddStock()` does NOT fire a domain event (known gap — see issues).

## API Endpoints (`/api/inventory`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/items` | CreateInventoryItem | Authenticated |
| `POST` | `/items/{skuCode}/add-stock` | AddStock | Authenticated |
| `GET` | `/items/{skuCode}` | GetBySkuCode | Public |
| `GET` | `/items` | GetAll | Authenticated |
| `POST` | `/items/batch` | BatchLookupBySkuIds | Authenticated |
| `PUT` | `/items/{skuCode}/stock` | UpsertStock (idempotent) | Authenticated |

## Integration Events

### Consumed (Integration Commands)

| Command | Consumer | Action |
|:---|:---|:---|
| `SkuCreatedIntegrationEvent` | `SkuCreatedConsumer` | Creates InventoryItem for new SKU (qty=0) |
| `SkuDeletedEvent` | `SkuDeletedConsumer` | Deactivates InventoryItem (zeros AvailableQuantity, preserves Reserved) |
| `ReserveInventoryCommand` | `ReserveInventoryConsumer` | Calls `Reserve()` on each item, publishes `InventoryReservedEvent` or `InventoryReservationFailedEvent` |
| `CancelReservationCommand` | `CancelReservationConsumer` | Calls `Release()` on each item, publishes `InventoryReleasedEvent` |

### Published (via MassTransit)

| Event | Trigger | Consumer |
|:---|:---|:---|
| `InventoryReservedEvent` | Successful reservation | Ordering Saga (advances to payment) |
| `InventoryReleasedEvent` | Successful release | Ordering Saga (confirms compensation) |
| `InventoryReservationFailedEvent` | OutOfStockException during reservation | Ordering Saga (triggers cancellation) |

## Current Status & Known Issues

- ✅ SKU-aware: keyed by SkuId, not ProductId
- ✅ `SkuDeletedConsumer` deactivates items correctly
- ✅ Idempotent upsert endpoint for seeder/admin use
- ✅ `Deactivate()` preserves ReservedQuantity for in-flight orders
- 🟡 `AddStock()` does not fire domain event → Search index can't update InStock status
- 🟡 Legacy `ProductCreatedConsumer` may create phantom records with placeholder SkuId (should be retired)
- 🟡 ProductId unique index was removed (was blocking multi-SKU products)

---
*Last Updated: 2026-06-19*
