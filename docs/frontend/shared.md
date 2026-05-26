# Shared Components & Layout

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/shared/` |
| **Scope** | Global — used across all features |

## Component Structure

```
shared/
├── components/
│   ├── header/
│   │   ├── header.ts            # HeaderComponent — top nav bar
│   │   ├── header.html          # External template
│   │   ├── header.css
│   │   └── header.spec.ts       # ✅ Tests
│   ├── footer/
│   │   ├── footer.ts            # FooterComponent
│   │   ├── footer.html
│   │   └── footer.css
│   ├── mega-menu/
│   │   ├── mega-menu.ts         # MegaMenuComponent — category mega dropdown
│   │   ├── mega-menu.html
│   │   └── mega-menu.spec.ts    # ✅ Tests
│   ├── cart-drawer/
│   │   ├── cart-drawer.ts       # CartDrawerComponent — slide-out cart overlay
│   │   ├── cart-drawer.html
│   │   └── cart-drawer.css
│   ├── search-bar/
│   │   └── search-bar.ts        # SearchBarComponent — global search input
│   ├── toast-container/
│   │   ├── toast-container.ts   # ToastContainerComponent — notification toasts
│   │   └── toast-container.spec.ts  # ✅ Tests
│   ├── stock-indicator/
│   │   ├── stock-indicator.ts   # StockIndicatorComponent — in-stock/low-stock badge
│   │   └── stock-indicator.spec.ts  # ✅ Tests
│   └── breadcrumbs/
│       └── breadcrumbs.ts       # BreadcrumbsComponent — breadcrumb navigation
└── pages/
    └── not-found/
        ├── not-found.ts         # NotFoundComponent — 404 page
        └── not-found.spec.ts    # ✅ Tests
```

## Key Components

### HeaderComponent
- Top navigation bar with logo, search, auth links, cart icon
- Renders `MegaMenuComponent` for category navigation
- Cart icon shows badge with `CartStore.totalItems()`
- Auth state from `AuthStore` — shows login/register or profile/logout

### CartDrawerComponent
- Slide-out drawer overlay for quick cart access
- Bound to `CartStore.isDrawerOpen()`
- Shows `MiniCartComponent` content inline
- Triggered by `CartStore.showDrawer()` / `toggleDrawer()`

### MegaMenuComponent
- Category tree dropdown from header
- Uses `CategoryTreeService` (core service)
- Hierarchical category navigation

### SearchBarComponent
- Global search input
- Navigates to `/catalog?q=<query>` on submit
- Likely uses `CatalogStore.updateSearchQuery()`

### ToastContainerComponent
- Displays notification toasts
- Driven by `ToastService` (core service)

### StockIndicatorComponent
- Displays stock status badge (in-stock, low-stock, out-of-stock)
- Accepts `quantity` input

### BreadcrumbsComponent
- Dynamic breadcrumb trail
- Likely reads from route data or a breadcrumb service

### NotFoundComponent
- 404 page for wildcard routes (`**`)

## Core Services (used globally)

| Service | Path | Purpose |
|:---|:---|:---|
| `ToastService` | `core/services/` | Toast notifications |
| `CategoryTreeService` | `core/services/` | Category hierarchy for mega-menu |
| `InventoryService` | `core/services/` | Stock checking |
| `NotificationService` | `core/signalr/` | SignalR real-time notifications |
| `LanguageService` | `core/` | i18n language switching |
| `ThemeService` | `core/` | Dark/light theme toggle |

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| `header/header.spec.ts` | ✅ | Passing |
| `mega-menu/mega-menu.spec.ts` | ✅ | Passing |
| `toast-container/toast-container.spec.ts` | ✅ | Passing |
| `stock-indicator/stock-indicator.spec.ts` | ✅ | Passing |
| `not-found/not-found.spec.ts` | ✅ | Passing |
| `cart-drawer/` | ❌ | **No tests** |
| `footer/` | ❌ | **No tests** |
| `search-bar/` | ❌ | **No tests** |
| `breadcrumbs/` | ❌ | **No tests** |

**E2E Coverage:** `not-found.spec.ts` (~2 tests). No header/navigation E2E tests.

## Known Gaps / Issues

- **CartDrawer has no tests:** Critical UI component — slide-out overlay with item management.
- **SearchBar has no tests:** Global search input — should test navigation and query passing.
- **Footer has no tests:** Lower priority but should verify links render.
- **Breadcrumbs has no tests:** Dynamic breadcrumb generation untested.
- **CartDrawer is a global component, not under cart feature:** This means it doesn't participate in cart feature's lazy loading — it's always loaded.
- **No layout wrapper component:** Layout (header + content + footer) is composed in `app.ts` root component, not a dedicated layout component. This makes nested layouts (e.g., seller dashboard) harder.
- **MegaMenu depends on CategoryTreeService:** If categories fail to load, the menu may be empty with no error state.
