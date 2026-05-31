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
| `InventoryItem` | Aggregate Root | SkuId (primary key), ProductId, StoreId, SkuCode, AvailableQuantity, ReservedQuantity, IsDeactivated, Version (optimistic concurrency) |

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

### Consumed

| Event | Consumer | Action |
|:---|:---|:---|
| `SkuCreatedIntegrationEvent` | `SkuCreatedConsumer` | Creates InventoryItem for new SKU |
| `SkuDeletedEvent` | `SkuDeletedConsumer` | Deactivates InventoryItem (zeros AvailableQuantity, preserves Reserved) |
| `ReserveInventoryEvent` | `ReserveInventoryConsumer` | Calls `Reserve()` on InventoryItem |
| `CancelReservationEvent` | `CancelReservationConsumer` | Calls `Release()` on InventoryItem |

### Published (via Outbox)

| Event | Trigger |
|:---|:---|
| `InventoryReservedEvent` | StockReservedDomainEvent |
| `InventoryReleasedEvent` | StockReleasedDomainEvent |
| `InventoryReservationFailedEvent` | OutOfStockException during reservation |

## Current Status & Known Issues

- ✅ SKU-aware: keyed by SkuId, not ProductId
- ✅ `SkuDeletedConsumer` deactivates items correctly
- ✅ Idempotent upsert endpoint for seeder/admin use
- 🟡 `AddStock()` does not fire domain event → Search index can't update InStock status
- 🟡 Legacy `ProductCreatedConsumer` may create phantom records with placeholder SkuId (should be retired)
- 🟡 ProductId unique index was removed (was blocking multi-SKU products)
