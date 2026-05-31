# Cart Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Concurrency** | Optimistic (PostgreSQL xmin) |
| **Project Path** | `src/Microservices/Cart/` |

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `ShoppingCart` | Aggregate Root | BuyerId (nullable for anonymous), Version (xmin), MaxItems=50 |
| `CartItem` | Child Entity | CartId, **ProductId**, **SkuId**, **SkuCode**, Quantity, Price, StoreId |
| `ProductPrice` | Entity (read model) | ProductId, **SkuId**, SkuCode, Name, Price, Currency, StoreId |

### SKU Refactor Status

Cart is **SKU-aware** as of the refactor:
- `CartItem` has `SkuId` and `SkuCode` fields
- `ShoppingCart.AddItem()` requires `(productId, skuId, skuCode)` composite identity
- `CartItem.MatchesProduct()` matches on `(ProductId, SkuId)` pair
- `ProductPrice` keyed by SkuId for price resolution

## API Endpoints (`/api/cart`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/` | `GetCartQuery` | Anonymous (BuyerId or X-Cart-Id) |
| `DELETE` | `/` | `DeleteCartCommand` | Anonymous |
| `POST` | `/checkout` | `CheckoutCartCommand` | Authenticated |
| `POST` | `/items` | `AddCartItemCommand` | Anonymous |
| `PUT` | `/items/{productId}/{skuId}` | `UpdateCartItemCommand` | Anonymous |
| `DELETE` | `/items/{productId}/{skuId}` | `RemoveCartItemCommand` | Anonymous |

**Anonymous cart support:** Uses `X-Cart-Id` header. Carts created anonymously can be claimed by authenticated users on checkout.

### Request DTOs

```csharp
record AddCartItemRequest(Guid ProductId, Guid SkuId, string SkuCode, int Quantity);
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
- ✅ Anonymous cart support with X-Cart-Id header
- ✅ Optimistic concurrency via PostgreSQL xmin
- 🟡 `SkuCreatedConsumer` needs verification — may not populate price from event correctly
- 🟡 Legacy `ProductUpdatedConsumer` removed (was overwriting prices with stale data)
