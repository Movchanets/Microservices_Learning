# Admin Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/admin/` |
| **Store Scope** | `AdminStore` — `providedIn: 'root'` (singleton) |
| **Route Prefix** | `/admin` |
| **Guard** | `authGuard` + `roleGuard('Admin')` |
| **Render Mode** | `RenderMode.Server` (SSR) |

## Component Structure

```
admin/
├── admin.store.ts              # AdminStore (root singleton)
├── admin.models.ts             # AdminUser, AdminStore, VerifyStoreRequest
├── admin.routes.ts             # Default export — nested children
├── admin-user.service.ts       # HTTP service → User management API
├── admin-store.service.ts      # HTTP service → Store management API
├── admin-page/
│   └── admin-page.ts           # AdminPageComponent — shell with nav
├── user-list/
│   └── user-list.ts            # UserListComponent — user table with actions
├── store-verification/
│   └── store-verification.ts   # StoreVerificationComponent — pending store approvals
├── store-detail/
│   └── store-detail.ts         # StoreDetailComponent — single store view + actions
└── components/
    └── stats-card/
        └── stats-card.ts       # StatsCardComponent — metric display card
```

## SignalStore State Management

### AdminStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `users` | `AdminUser[]` | All platform users |
| `stores` | `AdminStoreModel[]` | All stores |
| `pendingStores` | `AdminStoreModel[]` | Stores awaiting verification |
| `selectedStore` | `AdminStoreModel \| null` | Currently viewed store |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |

**Computed signals:** `pendingCount`, `verifiedStores`, `rejectedStores`, `adminUsers`, `sellerUsers`, `buyerUsers`, `hasUsers`, `hasPendingStores`

**Key methods:** `loadUsers()`, `loadStores(status?)`, `loadPendingStores()`, `loadStoreById(id)`, `verifyStore(storeId, request)`, `updateUserRole(userId, role)`, `deactivateUser(userId)`, `clearSelected()`, `clearError()`

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/admin` | `AdminPageComponent` | `authGuard` + `roleGuard('Admin')` |
| `/admin` → redirect | → `/admin/users` | |
| `/admin/users` | `UserListComponent` | (inherited) |
| `/admin/verifications` | `StoreVerificationComponent` | (inherited) |
| `/admin/stores` | `StoreVerificationComponent` | (inherited) — **same component as verifications** |
| `/admin/stores/:id` | `StoreDetailComponent` | (inherited) |

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| All admin specs | ❌ | **0 unit tests** |

**E2E Coverage:** Partially covered — `admin-panel.spec.ts` (~6 tests). Only panel display. Missing: store approval/rejection, user role change, user deactivation, refund flow.

## Known Gaps / Issues

- **Zero unit tests across entire feature:** `AdminStore`, `AdminUserService`, `AdminStoreService`, and all components have no tests.
- **`/admin/stores` and `/admin/verifications` share the same component:** Both routes load `StoreVerificationComponent`. The component likely differentiates by checking the route, but this is fragile.
- **No refund management:** The `AdminStore` has no methods for payment refunds — this was a deleted E2E test that was never re-implemented.
- **No pagination for users/stores:** `loadUsers()` and `loadStores()` load all records — no server-side pagination.
- **No search/filter for users:** `UserListComponent` loads all users with no filtering by role, status, or search query.
- **Role change is immediate:** `updateUserRole()` has no confirmation dialog or audit trail visible in the UI.
- **Store verification:** `verifyStore()` takes `VerifyStoreRequest` with `isApproved` boolean and optional `reason` — the rejection reason UI is not visible in the component tree.
