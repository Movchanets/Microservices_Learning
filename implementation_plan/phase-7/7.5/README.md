# Phase 7.5 — Seller Dashboard Frontend

**Goal**: Build the seller dashboard with product management, store settings, and sales overview.

## Sub-Plans

| # | File | Description |
|---|------|-------------|
| 7.5.1 | `7.5.1-seller-models-service.md` | Seller models, Catalog service (product CRUD), Store service |
| 7.5.2 | `7.5.2-seller-store.md` | NgRx SignalStore for seller products and store state |
| 7.5.3 | `7.5.3-seller-dashboard-ui.md` | Dashboard layout, product list, add/edit product, store settings |

## Dependencies
- Phase 2 backend (Catalog.API with product CRUD endpoints)
- Phase 6 backend (StoreManagement.API) — may be stubbed if not yet implemented
- Phase 7.0 (Angular project setup)
- Phase 7.1 (Auth — seller role)

## Key Decisions
- Seller operations go through Catalog.API for product CRUD
- Store settings may be stubbed until Phase 6 (StoreManagement) is built
- Sales overview requires order data — may be deferred to a later iteration
