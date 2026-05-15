# Phase 7.6 — Admin Panel Frontend

**Goal**: Build the admin panel with user management, seller verification, and platform overview.

**Status**: ✅ Implemented (2026-05-15)

## Sub-Plans

| # | File | Description |
|---|------|-------------|
| 7.6.1 | `7.6.1-admin-models-service.md` | Admin models, UserService (list/role management), StoreAdminService (verify/reject sellers) |
| 7.6.2 | `7.6.2-admin-store.md` | NgRx SignalStore for admin state (users, pending stores) |
| 7.6.3 | `7.6.3-admin-dashboard-ui.md` | Admin dashboard layout, user management table, seller verification queue |

## Dependencies
- Phase 1 backend (Identity.API with user endpoints — role management)
- Phase 6 backend (StoreManagement.API — seller verification endpoints)
- Phase 7.0 (Angular project setup)
- Phase 7.1 (Auth — admin role)

## BFF Routing Rules
- **Users**: `/api/identity/users/**` → `identity-api` (admin-only endpoints)
- **Stores**: `/api/stores/**` → `store-api` (Phase 6 — verification endpoints)

## Key Decisions
- Admin routes guarded by `role === 'Admin'` check
- Seller verification uses the `POST /api/stores/{id}/verify` endpoint from Phase 6
- User management uses Identity.API admin endpoints (may need to be added if not yet present)
- Admin dashboard uses same layout pattern as seller dashboard (sidebar nav + router-outlet)
