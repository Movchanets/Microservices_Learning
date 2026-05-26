# Product/SKU Refactor Status

> **Last Updated:** 2026-05-26
> **Source:** `plans/product-sku-refactor/`, `DDD_EventAlignment_Audit.md`

---

## Overview

The SKU refactoring decomposes the monolithic `Product` aggregate (which had Price + Sku as flat fields) into a clean `Product` (definition) + `SKU` (sellable variant) structure with category-bound attribute definitions and event-driven Inventory integration.

**Total Tasks:** 62 across 9 phases

---

## Phase Progress

| Phase | Title | Tasks | Status |
|:---:|:---|:---:|:---:|
| 1 | Domain Model Design | 10 | ✅ Complete |
| 2 | SharedContracts Updates | 4 | ❌ Pending |
| 3 | Domain Layer Implementation | 11 | ❌ Pending |
| 4 | Infrastructure Layer Implementation | 8 | ❌ Pending |
| 5 | Application Layer (Commands/Queries) | 10 | ❌ Pending |
| 6 | Inventory Service Updates | 4 | ❌ Pending |
| 7 | API Endpoints & Program.cs | 3 | ❌ Pending |
| 8 | Testing | 7 | ❌ Pending |
| 9 | Seed Data & Migration Verification | 5 | ❌ Pending |

**Completion:** 1/9 phases (10/62 tasks)

---

## Critical Path

```
Phase 1 ✅ ──▶ Phase 3 ──▶ Phase 4 ──▶ Phase 5 ──▶ Phase 7 ──▶ Phase 8 ──▶ Phase 9
                 │                       │
Phase 2 ─────────┘───────────────────────┘
                 │
                 └──▶ Phase 6 ──┘
```

---

## Phase Details

### Phase 1: Domain Model Design ✅

**Status:** Complete  
**Key Decisions:**
- SKU is a **child entity** of Product (not separate aggregate root)
- Price moves from Product → SKU
- Hybrid attribute storage: `TypedAttributes` (GIN-indexed) + `FlexibleAttributes` (no index)
- `AttributeDefinition` binds to Category, not Product
- `InventoryItem` references `SkuId` instead of `ProductId`
- Pre-production: single migration with data backfill

### Phase 2: SharedContracts Updates ❌

**Remaining Tasks:**
- [ ] Create `SkuCreatedIntegrationEvent` in SharedContracts ✅ (already exists)
- [ ] Update `ProductCreatedEvent` (remove Sku/Price, add DefaultPrice)
- [ ] Add `SkuStatus` enum to Catalog.Domain
- [ ] Add `AttributeTarget` and `AttributeType` enums

### Phase 3: Domain Layer Implementation ❌

**Remaining Tasks:**
- [ ] Create `Catalog.Domain/Entities/Sku.cs` (child entity) ✅ (already exists)
- [ ] Create `Catalog.Domain/Entities/AttributeDefinition.cs`
- [ ] Create `Catalog.Domain/Enums/SkuStatus.cs`
- [ ] Refactor `Product.cs` aggregate (remove Price/Sku, add Skus collection)
- [ ] Refactor `Category.cs` (add AttributeDefinitions collection)
- [ ] Create `SkuCreatedDomainEvent.cs`, `SkuDeletedDomainEvent.cs`
- [ ] Create `ISkuRepository` interface

### Phase 4–9: Not Started

See `plans/product-sku-refactor/task_plan.md` for full task breakdown.

---

## What Was Done (Phases 1–8 code changes)

The SKU refactor introduced code changes across multiple layers:

| Layer | Changes |
|:---|:---|
| Domain | `Sku` entity created, `Product.AddSku()`/`RemoveSku()` added, `SkuCreatedDomainEvent`/`SkuDeletedDomainEvent` added |
| SharedContracts | `SkuCreatedIntegrationEvent`, `SkuDeletedEvent`, `SkuPriceChangedEvent` created |
| Infrastructure | `SkuCreatedDomainEventHandler` created, EF migrations for Cart/Inventory/Ordering |
| Application | `AddSkuCommand`, `ChangePriceCommand` handlers created |
| API | SKU endpoints (`POST /{id}/skus`, `DELETE /{id}/skus/{skuId}`, `PATCH /{id}/skus/{skuId}/price`) |
| Inventory | `SkuCreatedConsumer` created, `InventoryItem` updated with `SkuId` |

---

## Critical Gaps (Post-Refactor)

> Source: `DDD_EventAlignment_Audit.md` (2026-05-25)

### 🔴 Events Defined But Never Published

| Event | Exists in SharedContracts | Has Domain Event | Has Handler | Published? |
|:---|:---:|:---:|:---:|:---:|
| `SkuDeletedEvent` | ✅ | ✅ | ❌ Missing | ❌ Never |
| `SkuPriceChangedEvent` | ✅ | ❌ Missing | ❌ Missing | ❌ Never |
| `ProductPriceChangedEvent` | ✅ | ❌ | ❌ | ❌ Never |

### 🔴 Missing Consumers

| Event | Inventory | Search | Cart |
|:---|:---:|:---:|:---:|
| `SkuCreatedIntegrationEvent` | ✅ | ❌ | ❌ |
| `SkuDeletedEvent` | ❌ | ❌ | ❌ |
| `SkuPriceChangedEvent` | ❌ | ❌ | ❌ |

### 🔴 Data Model Gaps

| Service | Issue |
|:---|:---|
| **Cart** | `CartItem` has no `SkuId` — keyed by `ProductId` only |
| **Search** | `ProductSearchDocument` has single Price/Sku — no multi-SKU support |
| **SharedContracts** | `OrderItemContract` has no `SkuId` — can't trace order items to variants |
| **Inventory** | `ReserveStockCommandHandler` looks up by `ProductId`, not `SkuId` |

---

## Recommended Fix Order

| # | Fix | Risk | Effort |
|:---:|:---|:---:|:---:|
| 1 | Create `SkuDeletedDomainEventHandler` + consumers in Inventory/Search/Cart | 🔴 | S |
| 2 | Add `SkuPriceChangedDomainEvent` to Sku + handler + consumers in Cart/Search | 🔴 | M |
| 3 | Add `SkuId`/`SkuCode` to `CartItem`, `ProductPrice`, `OrderItemContract` | 🔴 | L |
| 4 | Remove unique index on `InventoryItem.ProductId` | 🟠 | S |
| 5 | Add `SkuCreatedConsumer` to Search and Cart | 🟠 | M |
| 6 | Switch `ReserveStockCommandHandler` to resolve by SkuId | 🟠 | S |
| 7 | Clean up backward-compat fields from `ProductCreatedEvent`/`ProductUpdatedEvent` | 🟡 | M |
| 8 | Add `StockAddedDomainEvent` for search index sync | 🟡 | S |
| 9 | Remove/retire Inventory `ProductCreatedConsumer` (legacy path) | 🔵 | S |

---

## Schema Changes Summary

### Products Table (after refactor)

| Column | Status |
|:---|:---|
| `PriceAmount`, `PriceCurrency`, `Sku` | ❌ Removed |
| `Brand` (varchar 100) | ✅ Added |
| Id, Name, Description, Tags, Status, CategoryId, StoreId, ImageUrl, CreatedAt, UpdatedAt | Unchanged |

### Skus Table (new)

| Column | Type | Notes |
|:---|:---|:---|
| `Id` | uuid PK | — |
| `ProductId` | uuid FK → Products | CASCADE delete |
| `SkuCode` | varchar(50) | Unique |
| `PriceAmount` | numeric(18,2) | — |
| `PriceCurrency` | varchar(3) | ISO code |
| `Status` | int | SkuStatus enum |
| `TypedAttributes` | jsonb | GIN-indexed (jsonb_path_ops) |
| `FlexibleAttributes` | jsonb | No index |
| `CreatedAt`, `UpdatedAt` | timestamp | — |

### AttributeDefinitions Table (new)

| Column | Type | Notes |
|:---|:---|:---|
| `Id` | uuid PK | — |
| `CategoryId` | uuid FK → Categories | CASCADE delete |
| `Key` | varchar(64) | Unique per category |
| `DisplayName` | varchar(128) | — |
| `Target` | int | Product=0, Sku=1 |
| `ValueType` | int | Text=0, Number=1, Select=2 |
| `IsFilterable` | bool | Controls TypedAttributes vs FlexibleAttributes |
| `IsRequired` | bool | Validated on SKU creation |
| `SortOrder` | int | — |
| `AllowedValues` | jsonb | For Select type |
