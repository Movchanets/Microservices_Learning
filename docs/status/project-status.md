# Project Status

> **Last Updated:** 2026-05-26
> **Sources:** `problems.md`, `aspire-problems.md`

---

## Build Status

| Metric | Value |
|:---|:---|
| Solution builds | ✅ 0 errors |
| Build warnings | 26 (`.WithOpenApi()` deprecation notices, test infrastructure nits) |
| NuGet vulnerabilities | 0 CVEs |
| Angular build | ✅ Passes |
| Unit tests | 266 pass |

---

## Service Health (Aspire)

| Service | Status | Notes |
|:---|:---:|:---|
| Identity.API | ✅ Healthy | — |
| Catalog.API | ✅ Healthy | — |
| Search.API | ✅ Healthy | — |
| Cart.API | ✅ Healthy | Migrations created for SKU refactor |
| Inventory.API | ✅ Healthy | Migrations created for SKU refactor |
| Ordering.API | ✅ Healthy | Migrations created for SKU refactor |
| Payment.API | ✅ Healthy | — |
| StoreManagement.API | ✅ Healthy | — |
| Media.API | ✅ Healthy | — |
| Notification.Worker | ✅ Healthy | — |
| Angular Frontend | ⚠️ Intermittent | Exits with code 1 (port conflict or dependency on crashed gateway) |
| Scalar Container | ❌ Failed | `scalarapi/aspire-api-reference:0.9.34` — non-blocking (API docs only) |
| pgAdmin | ✅ Healthy | Was temporarily unhealthy during startup — self-healed |
| Cart DB | ✅ Healthy | Was unhealthy (DB not created due to API crash) — now healed |

---

## What Works

- ✅ Solution builds cleanly (0 errors, 0 CVEs)
- ✅ All microservices start and register with Aspire
- ✅ E2E test suite: 40 tests pass
- ✅ Angular SPA compiles and serves
- ✅ EF migrations created for Cart, Inventory, Ordering SKU refactor
- ✅ Unit tests: 266 passing
- ✅ API Gateway / YARP BFF routing functional
- ✅ MassTransit event bus operational (RabbitMQ)
- ✅ SignalR notifications via Redis backplane
- ✅ OpenTelemetry + Health Checks via ServiceDefaults

---

## What Is Broken

### 🔴 P0 — Blocking

| Issue | Impact | Status |
|:---|:---|:---:|
| **Seeder AddSku 409 Conflict** | Products created but never get SKUs → no products in catalog → cart/checkout fails | ❌ Open |

**Details:** `POST /api/catalog/products/{id}/skus` returns 409. An unhandled `InvalidOperationException` escapes `AddSkuHandler`. The `GlobalExceptionMiddleware` maps it to 409. OTEL structured logging deduplicates the error message, hiding the actual cause. Likely the `AddSkuValidator` regex or category's `ValidateRequiredAttributes()` throws.

**Fix approach:** Disable OTEL log dedup or add try-catch logging in `AddSkuHandler` to find actual error message.

### 🟡 P1 — Non-blocking

| Issue | Impact | Status |
|:---|:---|:---:|
| Seller dashboard E2E tests fail (3) | Auth/routing issue in Angular seller pages | ❌ Open |
| Catalog browse E2E tests fail (2) | Depends on P0 (seeder needs to populate products) | ❌ Blocked by P0 |
| Product-SKU CRUD E2E test fails (1) | Depends on seller dashboard fix | ❌ Blocked by P1 |
| Angular frontend intermittent exit | Port conflict or dependency crash | ⚠️ Needs investigation |
| Duplicate AppHost instances | Port conflicts possible | ⚠️ Manual fix: `taskkill /PID 48264 /F` |

---

## E2E Test Results

| Metric | Count |
|:---|:---:|
| Passed | 40 |
| Failed | 6 |
| Skipped | 10 |
| Did not run | 7 |

### Failed Tests

| Test File | Test Name | Root Cause |
|:---|:---|:---|
| `catalog/browse-products.spec.ts` | should display product list on catalog page | No products (seeder P0) |
| `catalog/browse-products.spec.ts` | should navigate to product detail | No products (seeder P0) |
| `seller/product-sku-crud.spec.ts` | should create a product without SKUs | Seller dashboard auth/UI |
| `seller/seller-dashboard.spec.ts` | should show seller dashboard for seller users | Heading not visible — auth/routing |
| `seller/seller-dashboard.spec.ts` | should navigate to seller products | Same visibility issue |
| `seller/seller-dashboard.spec.ts` | should navigate to store settings | "Store Settings" heading not found |

---

## Resolved Issues (This Session)

| Issue | Fix | File |
|:---|:---|:---|
| Inventory API — stock endpoints return 500 | Generate `SkuId` via `Guid.CreateVersion7()` when empty | `Inventory.API/Endpoints/InventoryEndpoints.cs:86` |
| Cart API — ProductCreatedConsumer duplicate key FAULT | Skip processing when `evt.Sku` is empty | `Cart.Infrastructure/Messaging/Consumers/ProductCreatedConsumer.cs:18` |
| Angular build failure — Missing DecimalPipe | Added `DecimalPipe` import | `src/web/src/app/features/seller-dashboard/product-form/product-form.ts:30` |
| NuGet Security Vulnerabilities (136 warnings) | Updated OpenTelemetry packages to latest | `Marketplace.ServiceDefaults.csproj` |
| Angular Compiler Warnings | Fixed NG8102, NG8113, NG8107 warnings | Various `.ts` files |
| Cart/Inventory/Ordering crash on startup (exit code -532462766) | Created EF migrations for pending model changes | Migrations for Cart, Inventory, Ordering |

---

## Configuration Notes

| Item | Status |
|:---|:---|
| Docker Daemon | Installed but not running — container restarts may fail |
| MediatR License | Warning in inventory-api, store-api — no impact in dev, required for production |
| AppHost PIDs | Two detected (49868, 48264) — kill stale: `taskkill /PID 48264 /F` |

---

## Priority Fix Order

| Priority | Task | Effort |
|:---:|:---|:---|
| 🔴 P0 | Fix seeder AddSku 409 — find actual error message | S |
| 🟡 P1 | Fix seller dashboard E2E tests (3 failures) — auth/routing | M |
| 🟡 P2 | Fix catalog browse E2E tests (2 failures) — depends on P0 | S |
| 🟢 P3 | Fix product-sku-crud E2E test (1 failure) — depends on P1 | S |
