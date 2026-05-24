# Backend State — 2026-05-16

## Overview

10 microservices + 1 API Gateway, all registered in Aspire AppHost. Clean Architecture with CQRS (MediatR), EF Core, MassTransit messaging.

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
| POST /api/cart/checkout | Auth | ✅ Triggers saga |

**TODOs:**
- ❌ No single-item endpoints (POST /items, PUT /items/{sku}, DELETE /items/{sku})
- ❌ Cart uses PostgreSQL instead of Redis (plan says Redis)

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

---

### 7. Payment.API ✅ Implemented

| Endpoint | Auth | Status |
|----------|------|--------|
| GET /api/payments/order/{orderId} | Auth | ✅ Working |

**TODOs:**
- ❌ No refund endpoint
- ❌ No payment initiation endpoint (payments triggered by saga only)

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
- **No auth middleware** (no UseAuthentication/UseAuthorization)
- Uses custom BuyerIdUserIdProvider from x-buyer-id header

**TODOs:**
- ❌ Events broadcast to all users (should target specific user)
- ⚠️ No authentication on SignalR hub

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
| Cart → OrderSubmittedEvent → Ordering Saga | ✅ Working |
| Ordering Saga → Inventory reservation | ✅ Working |
| Ordering Saga → Payment processing | ✅ Working |
| Ordering Saga → OrderCreatedEvent | ✅ Working |
| Catalog → ProductCreated/Updated/Deleted → Search | ✅ Working |
| StoreManagement → StoreVerifiedEvent → Identity (role update) | ✅ Working |
| Cart checkout Outbox | ✅ MassTransit Outbox configured |

---

## Test Coverage

| Type | Projects | Tests | Status |
|------|----------|-------|--------|
| Unit Tests | 12 (10 active) | ~160 methods | ✅ Domain + Application well covered |
| Integration Tests | 12 (6 active) | ~29 methods | ⚠️ 5 empty (Media, Notification, Ordering, Payment, StoreManagement) |
| E2E Tests | 1 suite | ~32 cases | ⚠️ 9 spec files, missing full checkout flow |

**Unit test coverage by layer:**
- Domain: 80-100% across all services
- Application/Handlers: All handlers tested
- Infrastructure: JWT, password hasher, middlewares tested
- **Media.UnitTests: EMPTY** (no tests at all)

**Integration test gaps:**
- Media, Notification, Ordering, Payment, StoreManagement have no integration tests

**E2E test gaps:**
- No full end-to-end checkout flow (browse → add → checkout → confirm)
- No payment flow E2E
- No order creation E2E
- Checkout spec only checks page load
- api-helpers.ts and db-helpers.ts are stubs (empty method bodies)
