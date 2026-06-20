# Cart Service

> **Last Updated:** 2026-06-20

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Concurrency** | Optimistic (PostgreSQL `xmin` system column) |
| **Project Path** | `src/Microservices/Cart/` |

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `ShoppingCart` | Aggregate Root | BuyerId (nullable Guid for anonymous), Version (xmin), CreatedAt, UpdatedAt, MaxItems=50 |
| `CartItem` | Child Entity | CartId, ProductId, SkuId, SkuCode, Quantity, Price, StoreId |
| `ProductPrice` | Entity (read model) | ProductId, SkuId, SkuCode, Name, Price, Currency, StoreId, UpdatedAt |

### SKU Refactor Status

Cart is **SKU-aware** as of the refactor:
- `CartItem` has `SkuId` and `SkuCode` fields
- `ShoppingCart.AddItem()` requires `(productId, skuId, skuCode)` composite identity
- `CartItem.MatchesProduct()` matches on `(ProductId, SkuId)` pair
- `ProductPrice` keyed by SkuId for price resolution

## API Endpoints (`/api/cart`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/` | `GetCartQuery` | Anonymous (BuyerId from JWT or `X-Cart-Id` header) |
| `DELETE` | `/` | `DeleteCartCommand` | Anonymous |
| `POST` | `/checkout` | `CheckoutCartCommand` | Authenticated (body: address fields) |
| `POST` | `/items` | `AddCartItemCommand` | Anonymous (body: `{ productId, skuId, skuCode, quantity }`) |
| `PUT` | `/items/{productId:guid}/{skuId:guid}` | `UpdateCartItemCommand` | Anonymous (body: `{ quantity }`) |
| `DELETE` | `/items/{productId:guid}/{skuId:guid}` | `RemoveCartItemCommand` | Anonymous |

**Anonymous cart support:** Uses `X-Cart-Id` header. Carts created anonymously can be claimed by authenticated users on checkout.

### Request DTOs

```csharp
record AddCartItemRequest(Guid ProductId, Guid SkuId, string SkuCode, int Quantity);
record UpdateCartItemRequest(int Quantity);
record CheckoutRequest(string? AddressLine1, string? AddressLine2, string? City, string? State, string? PostalCode, string? Country);
```

## Integration Events

### Consumed

| Event | Consumer | Action |
|:---|:---|:---|
| `SkuCreatedIntegrationEvent` | `SkuCreatedConsumer` | Creates ProductPrice entry per-SKU |
| `SkuDeletedEvent` | `SkuDeletedConsumer` | Removes ProductPrice, invalidates carts containing deleted SKU |
| `SkuPriceChangedEvent` | `SkuPriceChangedConsumer` | Updates ProductPrice, refreshes cart item prices |
| `ProductDeletedEvent` | `ProductDeletedConsumer` | Removes all ProductPrice entries for product |

### Published (via Outbox)

| Event | Trigger |
|:---|:---|
| `OrderSubmittedEvent` | Checkout — triggers Ordering saga |

## Current Status & Known Issues

- ✅ SKU-aware model with composite (ProductId, SkuId) identity
- ✅ All SKU-level consumers created (SkuCreated, SkuDeleted, SkuPriceChanged)
- ✅ ProductDeleted consumer for catalog cleanup
- ✅ Anonymous cart support with X-Cart-Id header
- ✅ Optimistic concurrency via PostgreSQL xmin
- ⚠️ `SkuCreatedConsumer` needs verification — may not populate price from event correctly
