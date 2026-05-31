# Stores Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/stores/` |
| **Store Scope** | No dedicated store — uses `StoreService` from seller-dashboard |
| **Route Prefix** | `/stores` |
| **Guard** | None (public) |
| **Render Mode** | `RenderMode.Prerender` |

## Component Structure

```
stores/
├── store.routes.ts             # Named export: STORE_ROUTES
└── store-page/
    └── store-page.ts           # StorePageComponent — public store page
```

## State Management

No dedicated SignalStore. The `StorePageComponent` likely uses `StoreService` (from `seller-dashboard/`) directly or injects `CatalogService` to load store products.

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/stores/:id` | `StorePageComponent` | None (public) |

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| All store specs | ❌ | **0 unit tests** |

**E2E Coverage:** **Not covered** — all E2E specs deleted (store-fixtures — 5 tests). Zero E2E coverage.

## Known Gaps / Issues

- **Minimal feature:** Only 2 files total — a route file and a single component. Feature is likely incomplete or very thin.
- **No store browsing/listing:** There is no `/stores` list page — only direct store access via `/stores/:id`.
- **No store search:** Users cannot search for stores.
- **No dedicated store state:** No `StorePageStore` — the component must fetch store info and products independently.
- **Cross-feature dependency:** Uses `StoreService` from `seller-dashboard/` — shared service across features.
- **No store reviews/ratings:** No mechanism for buyers to rate or review stores (only products have reviews).
