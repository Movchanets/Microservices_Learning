# Project State — 2026-05-16

## Files

| File | Description |
|------|-------------|
| [backend-state.md](backend-state.md) | All 10 microservices status, endpoints, auth, TODOs |
| [frontend-state.md](frontend-state.md) | Angular features, stores, components, guards |
| [flow-analysis.md](flow-analysis.md) | 10 key flows analyzed objectively |
| [todos-and-gaps.md](todos-and-gaps.md) | All TODOs prioritized (P1: 8, P2: 10, DevOps: 4) |

## Quick Summary

**Backend:** 10/10 services implemented. All endpoints working. 4 backend TODOs. Search + Notification have no auth.

**Frontend:** All major features implemented. NgRx SignalStore everywhere. 20 frontend TODOs.

**Flows:** 7/10 fully working, 3 partially working (store creation, add to cart, profile).

**Tests:** ~160 unit tests (10 active projects), ~29 integration tests (6 active), ~32 E2E cases (9 specs). 5 empty integration test projects.

## Top Blockers

1. **Store creation circular dependency** — Need Seller role to create store, but role comes from store verification
2. **No single-item cart endpoints** — Full replacement is inefficient
3. **No "Add to Cart" on product detail** — Can't add products from detail page
4. **No address form in checkout** — Missing shipping info
5. **No order cancellation** — Command exists but no endpoint/UI
