# Home Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/home/` |
| **Store Scope** | `HomeStore` — `providedIn: 'root'` (singleton) |
| **Route Prefix** | `/home` |
| **Guard** | None (public) |
| **Render Mode** | `RenderMode.Prerender` |

## Component Structure

```
home/
├── home.store.ts                    # HomeStore (root singleton)
├── home.routes.ts                   # Named export: HOME_ROUTES
├── home-page/
│   └── home-page.ts                 # HomePageComponent — composes all sections
└── components/
    ├── hero-banner/
    │   └── hero-banner.ts           # HeroBannerComponent — main banner
    ├── product-carousel/
    │   └── product-carousel.ts      # ProductCarouselComponent — horizontal scroll
    ├── category-tiles/
    │   └── category-tiles.ts        # CategoryTilesComponent — category grid
    └── deal-of-the-day/
        └── deal-of-the-day.ts       # DealOfTheDayComponent — featured deal
```

## SignalStore State Management

### HomeStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `featuredProducts` | `ProductListItem[]` | Featured/promoted products |
| `newArrivals` | `ProductListItem[]` | Recently added products |
| `categories` | `Category[]` | Top-level active categories (max 8) |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |

**Key methods:** `loadFeatured()`, `loadNewArrivals()`, `loadCategories()`, `loadAll()` — `loadAll()` fires all three in parallel via `Promise.all()`

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/home` | `HomePageComponent` | None (public) |

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| All home specs | ❌ | **0 unit tests** |

**E2E Coverage:** **Not covered** — all E2E specs deleted (home-page — 6 tests). Zero E2E coverage.

## Known Gaps / Issues

- **Zero tests (unit + E2E):** Entire feature is untested.
- **No computed signals:** `HomeStore` has no `withComputed` — all state is raw.
- **Deal of the day:** The `DealOfTheDayComponent` exists but the store has no dedicated `dealOfTheDay` state — likely reuses `featuredProducts` or has its own internal logic.
- **`loadNewArrivals()` reuses `getProducts({ page: 1, pageSize: 8 })`:** This fetches from the general catalog API, not a dedicated "new arrivals" endpoint. Results depend on API sort order.
- **Category filtering:** `loadCategories()` filters to `isActive` and takes first 8 — no sorting by popularity or featured status.
- **No carousel state:** `ProductCarouselComponent` likely manages its own scroll position via local signals — no store involvement.
- **Prerender mode:** Static content — no personalization or user-specific recommendations on homepage.
