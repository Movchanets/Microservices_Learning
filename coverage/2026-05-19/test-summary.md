# Test Run Summary — 2026-05-19

**Solution:** Marketplace.slnx  
**Runtime:** .NET 10 / C# 14.1  
**Build:** 0 errors, 106 warnings (all NuGet vulnerability advisories)  

---

## Overall Result: ✅ ALL PASSING

| Metric | Value |
|:---|---:|
| **Total Tests** | **349** |
| **Passed** | **349** |
| **Failed** | **0** |
| **Skipped** | **0** |
| **Test Projects** | **21** (1 shared, 0 executable) |
| **E2E Spec Files** | **24** (not run — requires Aspire stack) |

---

## Unit Tests (11 projects — 243 tests)

| Project | Tests | Passed | Failed | Status |
|:---|---:|---:|---:|:---:|
| Ordering.UnitTests | 70 | 70 | 0 | ✅ |
| Identity.UnitTests | 45 | 45 | 0 | ✅ |
| Payment.UnitTests | 30 | 30 | 0 | ✅ |
| StoreManagement.UnitTests | 29 | 29 | 0 | ✅ |
| Catalog.UnitTests | 19 | 19 | 0 | ✅ |
| BuildingBlocks.Infrastructure.UnitTests | 16 | 16 | 0 | ✅ |
| Cart.UnitTests | 15 | 15 | 0 | ✅ |
| Inventory.UnitTests | 8 | 8 | 0 | ✅ |
| Notification.UnitTests | 7 | 7 | 0 | ✅ |
| ApiGateway.UnitTests | 7 | 7 | 0 | ✅ |
| Search.UnitTests | 4 | 4 | 0 | ✅ |
| BuildingBlocks.SharedContracts.UnitTests | 4 | 4 | 0 | ✅ |

**Subtotal: 254 passed / 0 failed**

---

## Integration Tests (8 projects — 44 tests)

| Project | Tests | Passed | Failed | Status |
|:---|---:|---:|---:|:---:|
| Cart.IntegrationTests | 14 | 14 | 0 | ✅ |
| Inventory.IntegrationTests | 8 | 8 | 0 | ✅ |
| Identity.IntegrationTests | 7 | 7 | 0 | ✅ |
| Search.IntegrationTests | 6 | 6 | 0 | ✅ |
| Catalog.IntegrationTests | 4 | 4 | 0 | ✅ |
| Ordering.IntegrationTests | 3 | 3 | 0 | ✅ |
| ApiGateway.IntegrationTests | 2 | 2 | 0 | ✅ |
| IntegrationTests.Shared | 0 | 0 | 0 | ℹ️ (shared infra, no tests) |

**Subtotal: 44 passed / 0 failed**

---

## Contract Tests (1 project — 51 tests)

| Project | Tests | Passed | Failed | Status |
|:---|---:|---:|---:|:---:|
| ContractTests | 51 | 51 | 0 | ✅ |

**Subtotal: 51 passed / 0 failed**

---

## E2E Tests (Playwright — 24 spec files)

**Status:** NOT RUN — requires full Aspire stack (databases, RabbitMQ, ES, Redis)

Spec files:
- admin/admin-panel.spec.ts, admin/admin-store-detail.spec.ts
- auth/forgot-password.spec.ts, auth/login.spec.ts, auth/profile.spec.ts
- cart/add-to-cart.spec.ts, cart-drawer.spec.ts
- catalog/browse-products.spec.ts
- checkout/checkout-flow.spec.ts, checkout-flow.spec.ts
- header.spec.ts, header-mega-menu.spec.ts
- inventory-management.spec.ts
- order-cancellation.spec.ts, orders/order-history.spec.ts
- payment-refund.spec.ts
- product-detail-enhanced.spec.ts
- profile-hub.spec.ts
- saga-aware-cancellation.spec.ts
- seller/seller-dashboard.spec.ts, seller/seller-products.spec.ts
- seller-order-correlation.spec.ts, seller-orders.spec.ts
- store-fixtures.spec.ts

---

## Build Warnings

All 106 warnings are NuGet vulnerability advisories (NU1902) for:
- `OpenTelemetry.Api` 1.14.0 — GHSA-g94r-2vxg-569j (moderate)
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.14.0 — GHSA-4625-4j76-fww9, GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933 (moderate)
- `SixLabors.ImageSharp` 3.1.8 — GHSA-rxmq-m78w-7wmc (moderate)
- `Microsoft.AspNetCore.Identity` — NU1510 pruning warning
- `Microsoft.Extensions.Hosting` — NU1510 pruning warning

No functional code warnings. No build errors.

---

## Notes

- All 19 test projects build and execute successfully on .NET 10.
- Integration tests use Testcontainers (PostgreSQL, RabbitMQ, Redis).
- Contract tests validate MassTransit event/command shapes across service boundaries.
- Solution file is `.slnx` format (not legacy `.sln`).
