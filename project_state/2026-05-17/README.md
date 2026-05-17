# Project State — 2026-05-17

## Files

| File | Description |
|------|-------------|
| [backend-state.md](backend-state.md) | All 10 microservices status, endpoints, auth, TODOs |
| [frontend-state.md](frontend-state.md) | Angular features, stores, components, guards |
| [flow-analysis.md](flow-analysis.md) | 10 key flows analyzed objectively |
| [todos-and-gaps.md](todos-and-gaps.md) | All TODOs from plans 01-09 + pre-existing gaps |
| [ordering-flow-audit.md](ordering-flow-audit.md) | Ordering flow audit with 5 fixed issues + 2 residual gaps |

## Ordering Flow Audit — 5 Fixes Applied

| # | Issue | Fix |
|---|-------|-----|
| 1 | Cart checkout dropped shipping address | `CartEndpoints.cs` now binds `CheckoutRequest` body and forwards address fields |
| 2 | SignalR buyer targeting broken (custom header) | Switched to query string transport (`?buyerId=`) |
| 3 | SignalR lifecycle only on app boot | `AuthStore` now starts/stops SignalR on login/register/checkAuth/logout |
| 4 | Order read model drifted from saga state | 4 projection consumers added (inventory reserved, payment processing, completed, cancelled) |
| 5 | Failed payments not persisted | `ProcessPaymentHandler` now records both success and failure outcomes |

### Residual Gaps

1. **Seller order correlation** — `OrderItem.SellerId` not reliably propagated during checkout
2. **Manual cancellation not saga-aware** — `CancelOrderHandler` doesn't coordinate with saga compensation

## Quick Summary

**Backend:** 10/10 services implemented. Ordering flow audit fixed 5 issues (address passthrough, SignalR targeting/lifecycle, order projection sync, payment failure persistence). Cart now has single-item endpoints (POST /items, PUT /items/{sku}, DELETE /items/{sku}).

**Frontend:** 293 Vitest tests passing (36 spec files). SignalR lifecycle integrated into AuthStore. Notification service uses query string for buyer identity.

**Tests:** Backend: 218 unit + 45 contract + 36 integration = 299 tests. Frontend: 293 Vitest. 6 Search.IntegrationTests failing (Elasticsearch not running). 18 E2E spec files.

**Key changes since 2026-05-16:**
- Ordering flow audit completed with 5 fixes
- Cart single-item endpoints added (AddCartItem, UpdateCartItem, RemoveCartItem)
- 4 ordering projection consumers added
- Contract test suite added (45 tests across 9 files)
- Ordering integration tests started (3 saga tests)
- E2E checkout-flow spec added

## Remaining Gaps

See [todos-and-gaps.md](todos-and-gaps.md) for full list. Key items:
- 2 ordering flow residual gaps (seller correlation, saga-aware cancellation)
- 4 missing features (photo upload, price alerts, filter chips, breadcrumbs)
- 5 performance/quality items (SQL aggregation, ES aggregation, tests)
- 1 P1 (refund endpoint)
- 16 P2 items
- 4 DevOps items (deferred)
