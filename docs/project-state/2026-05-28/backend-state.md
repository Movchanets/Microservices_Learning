# Backend State — 2026-05-28

> **Snapshot Date:** 2026-05-28  
> **Stack:** .NET 10, PostgreSQL, RabbitMQ, Redis, Elasticsearch

---

## Build Status

| Metric | Value |
|:---|:---|
| Solution builds | ✅ 0 errors |
| Build warnings | 26 (`.WithOpenApi()` deprecation, test nits) |
| NuGet vulnerabilities | 0 CVEs |

---

## Service Health & Test Counts

| # | Service | Status | Unit | Integration | Notes |
|:---:|:---|:---:|:---:|:---:|:---|
| 1 | Identity.API | ✅ | 45 | 7 | Auth, users, saved searches |
| 2 | Catalog.API | ✅ | 32 | 4 | Products, categories, SKUs, attributes |
| 3 | Cart.API | ✅ | 31 | 20 | SKU-based cart items |
| 4 | Inventory.API | ✅ | 8 | 5/8 ❌ | Reservation tests failing |
| 5 | Ordering.API | ✅ | 69 | 3 | Saga-based checkout |
| 6 | Payment.API | ✅ | 30 | — | ProcessPayment, refunds |
| 7 | StoreManagement.API | ✅ | 29 | — | Store CRUD, verification |
| 8 | Media.API | ✅ | — | — | Upload, gallery, thumbnails |
| 9 | Search.API | ✅ | 8 | 5/6 ❌ | Elasticsearch, currency mismatch |
| 10 | Notification.Worker | ✅ | 20 | — | SignalR hub |
| 11 | ApiGateway | ✅ | 7 | 2 | YARP, BFF, CookieToBearer |
| 12 | BuildingBlocks.Infrastructure | — | 16 | — | Shared infrastructure |

**Totals:** 295 unit ✅ | 46/50 integration (4 failed) | 66/67 contract (1 failed)

---

## Contract Test Failures

| Test | Root Cause |
|:---|:---|
| `CatalogToSearchContractTests.SkuCreatedIntegrationEvent_Contract_ShouldUpdatePriceInSearch` | Search doesn't consume SkuCreatedIntegrationEvent |

---

## Integration Test Failures

| Test | Service | Root Cause |
|:---|:---|:---|
| `ReserveInventory_MultipleItems_ReservesAllOrFails` | Inventory | Mock capture null |
| `ReserveInventory_SufficientStock_PublishesReservedEvent_AndReducesStock` | Inventory | Mock capture null |
| `CancelReservation_PublishesReleasedEvent_AndRestoresStock` | Inventory | Quantity mismatch (expected 10, got 6) |
| `UpdateProduct_VerifyNewFields` | Search | Currency mismatch (expected EUR, got USD) |

---

## Infrastructure

| Component | Status |
|:---|:---:|
| PostgreSQL | ✅ Healthy |
| RabbitMQ | ✅ Healthy |
| Redis | ✅ Healthy |
| Elasticsearch | ⚠️ Required for Search integration tests |
| OpenTelemetry | ✅ Configured |
| Health Checks | ✅ All services |
