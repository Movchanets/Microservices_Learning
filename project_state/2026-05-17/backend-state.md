# Backend State — 2026-05-17

## Overview

10 microservices + 1 API Gateway, all registered in Aspire AppHost. Clean Architecture with CQRS (MediatR), EF Core, MassTransit messaging.

**Build:** `dotnet build Marketplace.slnx` — 0 errors, 122 warnings (all NuGet vulnerability advisories).

---

## Service-by-Service Status

### 1. Identity.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| POST /api/identity/auth/register | Public | ✅ Working |
| POST /api/identity/auth/login | Public | ✅ Working |
| POST /api/identity/auth/refresh | Public | ✅ Working |
| POST /api/identity/auth/forgot-password | Public | ✅ Placeholder (no email) |
| GET /api/identity/users | Admin | ✅ Working |
| GET /api/identity/users/{id} | Auth | ✅ Working |
| PUT /api/identity/users/{id}/role | Admin | ✅ Working |
| DELETE /api/identity/users/{id} | Admin | ✅ Working |

**TODOs:**
- ❌ No change-password endpoint
- ❌ No update-profile endpoint
- ❌ Forgot-password sends no email (placeholder)

---

### 2. Catalog.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| GET /api/catalog/products | Public | ✅ Paginated, filterable |
| GET /api/catalog/products/{id} | Public | ✅ Working |
| POST /api/catalog/products | Auth | ✅ Working |
| PUT /api/catalog/products/{id} | Auth | ✅ Working |
| PATCH /api/catalog/products/{id}/price | Auth | ✅ Working |
| DELETE /api/catalog/products/{id} | Auth | ✅ Working |
| GET /api/catalog/categories | Public | ✅ Working |
| POST /api/catalog/categories | Auth | ✅ Working |
| PUT /api/catalog/categories/{id} | Auth | ✅ Working |
| DELETE /api/catalog/categories/{id} | Auth | ✅ Working |

**TODOs:**
- ❌ No seller-specific product filtering (storeId query param exists but no seller policy enforcement)

---

### 3. Search.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| GET /api/search/products | **No auth** | ✅ Elasticsearch with filters |

**TODOs:**
- ❌ No admin reindex endpoint
- ⚠️ No authentication configured at all (no UseAuthentication/UseAuthorization in pipeline)

---

### 4. Inventory.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| GET /api/inventory/items/{sku} | Public | ✅ Working |
| GET /api/inventory/items | Auth | ✅ Working |
| POST /api/inventory/items | Auth | ✅ Working |
| POST /api/inventory/items/{sku}/add-stock | Auth | ✅ Working |

**TODOs:**
- ❌ No low-stock alert mechanism
- ❌ No dedicated seller inventory management endpoint

---

### 5. Cart.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| GET /api/cart | Auth | ✅ Working |
| POST /api/cart | Auth | ✅ Full replacement |
| DELETE /api/cart | Auth | ✅ Working |
| POST /api/cart/checkout | Auth | ✅ Triggers saga, forwards address |
| POST /api/cart/items | Auth | ✅ **NEW** — Single item add |
| PUT /api/cart/items/{sku} | Auth | ✅ **NEW** — Single item update |
| DELETE /api/cart/items/{sku} | Auth | ✅ **NEW** — Single item remove |

**TODOs:**
- ⚠️ Cart uses PostgreSQL instead of Redis (plan says Redis)

**Changes since 2026-05-16:**
- Added `AddCartItemCommand`, `UpdateCartItemCommand`, `RemoveCartItemCommand` with validators
- Added `CheckoutRequest` body binding with address fields (AddressLine1, AddressLine2, City, State, PostalCode, Country)
- `CheckoutCartCommand` now forwards address fields into `OrderSubmittedEvent`

---

### 6. Ordering.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| POST /api/orders | Auth | ✅ Working |
| GET /api/orders/{id} | Auth | ✅ Working |
| GET /api/orders/buyer/{buyerId} | Auth | ✅ Working |
| GET /api/orders/seller/{sellerId} | Seller | ✅ Working |

**TODOs:**
- ❌ No cancel order endpoint (CancelOrderCommand exists but no endpoint)

**Changes since 2026-05-16:**
- Added 4 projection consumers to keep persisted `Order` in sync with saga:
  - `OrderInventoryReservedConsumer` — marks order as InventoryReserved
  - `OrderPaymentProcessingConsumer` — marks order as PaymentProcessing
  - `OrderCompletedProjectionConsumer` — marks order as Completed
  - `OrderCancelledProjectionConsumer` — marks order as Cancelled
- All consumers publish `OrderStatusChangedEvent` for SignalR notifications
- All consumers guard against idempotent re-processing

---

### 7. Payment.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| GET /api/payments/order/{orderId} | Auth | ✅ Working |

**TODOs:**
- ❌ No refund endpoint
- ❌ No payment initiation endpoint (payments triggered by saga only)

**Changes since 2026-05-16:**
- `ProcessPaymentHandler` now persists both successful AND failed payment transactions
- Previously only wrote successful transactions; failed ones returned empty from `/api/payments/order/{id}`

---

### 8. StoreManagement.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| POST /api/stores | Seller | ✅ Working |
| GET /api/stores | Public | ✅ With status filter |
| GET /api/stores/{id} | Public | ✅ Working |
| GET /api/stores/seller/{sellerId} | Public | ✅ Working |
| PUT /api/stores/{id} | Seller | ✅ Working |
| POST /api/stores/{id}/verify | Admin | ✅ Working |
| PUT /api/stores/{id}/logo | Auth | ✅ Working |

**TODOs:**
- ❌ No store deletion endpoint

---

### 9. Media.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| POST /api/media/upload | Auth | ✅ With thumbnail generation |
| GET /api/media/{blobName} | Public | ✅ Working |
| GET /api/media/{blobName}/thumbnail | Public | ✅ Working |
| GET /api/media | Auth | ✅ Working |
| DELETE /api/media/{blobName} | Auth | ✅ Working |

**TODOs:**
- None significant

---

### 10. Notification.Worker ✅ Implemented

- SignalR hub at /hubs/notifications
- Redis backplane for scaling
- OrderUpdate events broadcast
- **Buyer targeting via query string** (fixed from header-based approach)

**Changes since 2026-05-16:**
- `BuyerIdUserIdProvider` now resolves buyer identity from query string first, header second
- `NotificationHub.OnConnectedAsync` logs buyer identity from query string/header fallback
- `NotificationService` (frontend) sends `?buyerId=` in URL instead of custom header
- AuthStore now starts/stops SignalR on login/register/logout/checkAuth lifecycle

**TODOs:**
- ⚠️ No authentication middleware (no UseAuthentication/UseAuthorization)
- ❌ Events broadcast to all users (should target specific user via `Clients.User(buyerId)`)

---

### 11. API Gateway ✅ Implemented

| Feature | Status |
|---------|--------|
| YARP reverse proxy | ✅ All 10 services routed |
| Cookie-to-Bearer middleware | ✅ Working |
| CSRF validation | ✅ Working |
| Rate limiting | ✅ 100 req/min fixed window |
| CORS | ✅ localhost:4200/4201 |
| Health endpoints | ✅ All services |
| Auth policies | ✅ Authenticated/Seller/Admin |
| Request logging | ✅ Working |

**TODOs:**
- ❌ No token refresh in gateway

---

## Infrastructure

| Component | Status |
|-----------|--------|
| PostgreSQL | ✅ 7 databases (per-service) |
| Redis | ✅ Cart + SignalR backplane |
| RabbitMQ | ✅ MassTransit messaging |
| Elasticsearch | ✅ Search indexing |
| Azure Blob Storage | ✅ Azurite emulator locally |
| Aspire AppHost | ✅ All services registered |
| Scalar API Reference | ✅ All HTTP services |

---

## Messaging & Sagas

| Flow | Status |
|------|--------|
| Cart → OrderSubmittedEvent → Ordering Saga | ✅ Working (with address forwarding) |
| Ordering Saga → Inventory reservation | ✅ Working |
| Ordering Saga → Payment processing | ✅ Working |
| Ordering Saga → OrderCompleted/Cancelled | ✅ Working |
| Order projection sync (4 consumers) | ✅ **NEW** — keeps persisted Order in sync |
| Catalog → ProductCreated/Updated/Deleted → Search | ✅ Working |
| StoreManagement → StoreVerifiedEvent → Identity (role update) | ✅ Working |
| Cart checkout Outbox | ✅ MassTransit Outbox configured |

---

## Test Coverage

### Backend — 299 tests total

| Type | Projects | Tests | Status |
|------|----------|-------|--------|
| Unit Tests | 12 (11 active) | 218 | ✅ All passing |
| Contract Tests | 1 (9 test files) | 45 | ✅ All passing |
| Integration Tests | 7 (6 active) | 36 | ⚠️ Search.IntegrationTests (6) failing — needs Elasticsearch |

### Unit Test Breakdown

| Project | Tests | Status |
|---------|-------|--------|
| Identity.UnitTests | 45 | ✅ |
| Ordering.UnitTests | 64 | ✅ |
| StoreManagement.UnitTests | 29 | ✅ |
| Catalog.UnitTests | 19 | ✅ |
| BuildingBlocks.Infrastructure.UnitTests | 16 | ✅ |
| Payment.UnitTests | 14 | ✅ |
| Cart.UnitTests | 9 | ✅ |
| Inventory.UnitTests | 8 | ✅ |
| ApiGateway.UnitTests | 7 | ✅ |
| Search.UnitTests | 4 | ✅ |
| Notification.UnitTests | 3 | ✅ |

### Contract Test Breakdown

| File | Tests | Coverage |
|------|-------|---------|
| CheckoutFlowContractTests | 15 | Saga state machine happy path, compensation, address forwarding |
| CatalogToCartContractTests | 6 | Product price change propagation to cart |
| CatalogToInventoryContractTests | 4 | Product creation → inventory reservation |
| CatalogToSearchContractTests | 4 | Product CRUD → Elasticsearch indexing |
| IdentityContractTests | 4 | StoreVerified → role update |
| NotificationContractTests | 4 | Order events → SignalR notifications |
| OrderingConsumerContractTests | 4 | OrderSubmitted consumer behavior |
| InventoryReservationContractTests | 2 | Reserve/release inventory |
| PaymentContractTests | 2 | Process payment success/failure |

### Integration Test Breakdown

| Project | Tests | Status |
|---------|-------|--------|
| Identity.IntegrationTests | 7 | ✅ |
| Cart.IntegrationTests | 12 | ✅ |
| Inventory.IntegrationTests | 8 | ✅ |
| Catalog.IntegrationTests | 4 | ✅ |
| Ordering.IntegrationTests | 3 | ✅ **NEW** — saga integration tests |
| ApiGateway.IntegrationTests | 2 | ✅ |
| Search.IntegrationTests | 6 | ❌ All failing (Elasticsearch not running) |

### E2E Tests

- 18 spec files in `tests/E2ETests/tests/`
- Includes checkout-flow.spec.ts (new)
- Pre-existing registration/Playwright fill issue affects auth-dependent tests

---

## Known Issues

1. **Search.IntegrationTests (6 failures)** — All 6 tests fail because Elasticsearch is not running in the test environment. Tests need Testcontainers for ES or a running instance.
2. **NuGet vulnerabilities** — 122 warnings for OpenTelemetry packages (moderate severity). Needs package updates.
3. **BuildingBlocks.SharedContracts.UnitTests** — Project file not found (may have been removed/renamed).
