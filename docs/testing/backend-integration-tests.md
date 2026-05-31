# Backend Integration Test Inventory

**Project:** Marketplace Microservices
**Framework:** xUnit + Testcontainers (PostgreSQL, Redis) + WebApplicationFactory
**Last Updated:** 2026-05-26
**Total:** 13 test files, ~51 tests

---

## Test Projects

| Project | Path | Test Files | ~Tests |
|---------|------|-----------|--------|
| Cart.IntegrationTests | `tests/IntegrationTests/Cart.IntegrationTests/` | 3 | 10 |
| Catalog.IntegrationTests | `tests/IntegrationTests/Catalog.IntegrationTests/` | 4 | 15 |
| Identity.IntegrationTests | `tests/IntegrationTests/Identity.IntegrationTests/` | 5 | 10 |
| Inventory.IntegrationTests | `tests/IntegrationTests/Inventory.IntegrationTests/` | 3 | 10 |
| Ordering.IntegrationTests | `tests/IntegrationTests/Ordering.IntegrationTests/` | 2 | 8 |
| Search.IntegrationTests | `tests/IntegrationTests/Search.IntegrationTests/` | 3 | 8 |
| ApiGateway.IntegrationTests | `tests/IntegrationTests/ApiGateway.IntegrationTests/` | 1 | 2 |
| Shared | `tests/IntegrationTests/Shared/` | 2 | — |

---

## Cart.IntegrationTests (3 files, ~10 tests)

| Test File | What It Tests |
|-----------|---------------|
| `CartRepositoryTests.cs` | Cart repository CRUD with Redis persistence |
| `Consumers/CatalogEventConsumerTests.cs` | CatalogEventConsumer handles ProductUpdated events |
| `Fixtures/CartDatabaseFixture.cs` | Test fixture (Redis container setup) |

**Gaps:**
- Cart merge on login (anonymous → authenticated)
- Cart Redis TTL expiration
- Concurrent cart updates (race condition)

---

## Catalog.IntegrationTests (4 files, ~15 tests)

| Test File | What It Tests |
|-----------|---------------|
| `ProductRepositoryTests.cs` | Product repository CRUD with PostgreSQL |
| `CategoryRepositoryTests.cs` | Category repository CRUD |
| `OutboxIntegrationTests.cs` | Outbox pattern event publishing |
| `Fixtures/CatalogDatabaseFixture.cs` | Test fixture (PostgreSQL container setup) |

**Gaps:**
- Product search with Elasticsearch
- SKU CRUD operations
- Product activation/deactivation lifecycle

---

## Identity.IntegrationTests (5 files, ~10 tests)

| Test File | What It Tests |
|-----------|---------------|
| `IdentityDatabaseTests.cs` | Database schema and migrations |
| `UserRepositoryTests.cs` | User repository CRUD |
| `IdentityIntegrationTestBase.cs` | Shared test base class |
| `IdentityIntegrationCollection.cs` | xUnit collection definition |
| `Fixtures/IdentityDatabaseFixture.cs` | Test fixture (PostgreSQL container) |
| `NoOpPublisher.cs` | Test double for message publisher |

**Gaps:**
- Full registration flow with event publishing
- Login with password verification end-to-end
- Token refresh flow

---

## Inventory.IntegrationTests (3 files, ~10 tests)

| Test File | What It Tests |
|-----------|---------------|
| `InventoryItemRepositoryTests.cs` | Inventory item repository CRUD |
| `Consumers/ReservationConsumerTests.cs` | ReservationConsumer handles stock reservation events |
| `Fixtures/InventoryDatabaseFixture.cs` | Test fixture (PostgreSQL container) |

**Gaps:**
- Stock reservation with concurrent requests
- Stock release after order cancellation
- Low-stock threshold alerting

---

## Ordering.IntegrationTests (2 files, ~8 tests)

| Test File | What It Tests |
|-----------|---------------|
| `Saga/OrderSagaIntegrationTests.cs` | Order saga orchestration (create → payment → inventory) |
| `Fixtures/OrderingDatabaseFixture.cs` | Test fixture (PostgreSQL container) |

**Gaps:**
- Saga compensation on payment failure
- Saga compensation on inventory failure
- Order status update propagation

---

## Search.IntegrationTests (3 files, ~8 tests)

| Test File | What It Tests |
|-----------|---------------|
| `SearchQueryTests.cs` | Search query execution against PostgreSQL |
| `IndexingTests.cs` | Product indexing into search store |
| `Fixtures/SearchDatabaseFixture.cs` | Test fixture (PostgreSQL container) |

**Gaps:**
- Faceted search aggregation
- Search result ranking/relevance
- Index rebuild from catalog events

---

## ApiGateway.IntegrationTests (1 file, ~2 tests)

| Test File | What It Tests |
|-----------|---------------|
| `MiddlewarePipelineTests.cs` | Middleware pipeline execution order |

**Gaps:**
- Rate limiting behavior
- YARP proxy routing validation
- Authentication/authorization middleware integration

---

## Shared Test Infrastructure

| File | Purpose |
|------|---------|
| `Shared/TestDatabaseHelpers.cs` | Database seeding, cleanup, migration helpers |
| `Shared/AuthenticationTestHelpers.cs` | JWT token generation, auth header helpers |

---

## Infrastructure

All integration tests use **Testcontainers** for isolated database instances:

| Container | Used By |
|-----------|---------|
| PostgreSQL | Catalog, Identity, Inventory, Ordering, Search, Cart |
| Redis | Cart |

---

## How to Run

```bash
# All integration tests
dotnet test tests/IntegrationTests/ --verbosity normal

# Single service
dotnet test tests/IntegrationTests/Cart.IntegrationTests/ --verbosity normal

# With coverage
dotnet test tests/IntegrationTests/ --collect:"XPlat Code Coverage"
```

> **Note:** Integration tests require Docker running for Testcontainers.

---

## Missing Integration Test Projects

| Service | Status | Priority |
|---------|--------|----------|
| Payment | ❌ No project | P1 — payment processing with DB persistence untested |
| Notification | ❌ No project | P2 — event consumption tested only at unit level |
| StoreManagement | ❌ No project | P2 — store CRUD tested only at unit level |

---

*Generated from test source files in `tests/IntegrationTests/`.*
