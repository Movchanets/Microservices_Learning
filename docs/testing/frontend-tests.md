# Frontend Unit Test Inventory (Vitest)

**Project:** Marketplace Microservices — Angular Frontend
**Framework:** Vitest + Angular TestBed
**Last Updated:** 2026-05-26
**Total:** 36 spec files, 337 tests
**Command:** `pnpm test` (runs `ng test --watch=false`)

---

## Test Files by Feature Area

### Core Services & Auth (6 files)

| Test File | What It Tests |
|-----------|---------------|
| `core/auth/auth.store.spec.ts` | AuthStore — login, logout, token refresh state |
| `core/auth/auth.service.spec.ts` | AuthService — API calls, cookie handling |
| `core/http/api.interceptor.spec.ts` | API interceptor — withCredentials, error handling |
| `core/services/toast.service.spec.ts` | Toast notification service |
| `core/services/category-tree.service.spec.ts` | Category tree building and navigation |
| `core/signalr/notification.service.spec.ts` | SignalR notification hub connection |
| `core/theme.service.spec.ts` | Theme toggle (dark/light) |
| `core/language.service.spec.ts` | Language/i18n service |

### Cart Feature (4 files)

| Test File | What It Tests |
|-----------|---------------|
| `features/cart/cart.store.spec.ts` | CartStore — add, remove, update, checkout state |
| `features/cart/cart.service.spec.ts` | CartService — API calls for cart operations |
| `features/cart/cart-page/cart-page.spec.ts` | CartPageComponent — display, interactions |
| `features/cart/components/mini-cart/mini-cart.spec.ts` | MiniCartComponent — badge count, preview |

### Catalog Feature (2 files)

| Test File | What It Tests |
|-----------|---------------|
| `features/catalog/components/buy-box/buy-box.spec.ts` | BuyBoxComponent — price, SKU selection, add-to-cart |
| `features/catalog/components/frequently-bought-together/frequently-bought-together.spec.ts` | FrequentlyBoughtTogetherComponent |

### Checkout Feature (2 files)

| Test File | What It Tests |
|-----------|---------------|
| `features/checkout/checkout.store.spec.ts` | CheckoutStore — address, shipping, submit state |
| `features/checkout/checkout-page/checkout-page.spec.ts` | CheckoutPageComponent — form, validation |

### Auth Feature (1 file)

| Test File | What It Tests |
|-----------|---------------|
| `features/auth/login/login.spec.ts` | LoginComponent — form, validation, submission |
| `features/auth/register/register.spec.ts` | RegisterComponent — form, validation, submission |

### Orders Feature (5 files)

| Test File | What It Tests |
|-----------|---------------|
| `features/orders/order.store.spec.ts` | OrderStore — load, select, filter state |
| `features/orders/order.service.spec.ts` | OrderService — API calls |
| `features/orders/order-list/order-list.spec.ts` | OrderListComponent — list display |
| `features/orders/order-detail/order-detail.spec.ts` | OrderDetailComponent — detail display |
| `features/orders/order-timeline/order-timeline.spec.ts` | OrderTimelineComponent — status timeline |
| `features/orders/components/status-badge/status-badge.spec.ts` | StatusBadgeComponent — status colors/icons |

### Seller Dashboard Feature (5 files)

| Test File | What It Tests |
|-----------|---------------|
| `features/seller-dashboard/seller-product.store.spec.ts` | SellerProductStore — CRUD state |
| `features/seller-dashboard/seller-product.service.spec.ts` | SellerProductService — API calls |
| `features/seller-dashboard/inventory.store.spec.ts` | InventoryStore — stock management state |
| `features/seller-dashboard/inventory.service.spec.ts` | InventoryService — API calls |
| `features/seller-dashboard/seller-orders/seller-orders.spec.ts` | SellerOrdersComponent — order management |
| `features/seller-dashboard/inventory-list/inventory-list.spec.ts` | InventoryListComponent — stock display |

### Shared Components (5 files)

| Test File | What It Tests |
|-----------|---------------|
| `shared/components/header/header.spec.ts` | HeaderComponent — nav, search, cart button |
| `shared/components/mega-menu/mega-menu.spec.ts` | MegaMenuComponent — category navigation |
| `shared/components/toast-container/toast-container.spec.ts` | ToastContainerComponent — notification display |
| `shared/components/stock-indicator/stock-indicator.spec.ts` | StockIndicatorComponent — availability display |
| `shared/pages/not-found/not-found.spec.ts` | NotFoundComponent — 404 page |

### App Root (1 file)

| Test File | What It Tests |
|-----------|---------------|
| `app.spec.ts` | Root AppComponent — initialization, routing |

---

## Coverage by Store

| Store | Has Tests | File |
|-------|-----------|------|
| AuthStore | ✅ | `core/auth/auth.store.spec.ts` |
| CartStore | ✅ | `features/cart/cart.store.spec.ts` |
| CatalogStore | ❌ | *(no dedicated spec)* |
| CheckoutStore | ✅ | `features/checkout/checkout.store.spec.ts` |
| OrderStore | ✅ | `features/orders/order.store.spec.ts` |
| AdminStore | ❌ | *(no dedicated spec)* |
| SellerProductStore | ✅ | `features/seller-dashboard/seller-product.store.spec.ts` |
| StoreSettingsStore | ❌ | *(no dedicated spec)* |
| ProfileStore | ❌ | *(no dedicated spec)* |

---

## Gaps

| Gap | Priority | Notes |
|-----|----------|-------|
| CatalogStore tests | P1 | Feature-scoped store has no dedicated spec |
| AdminStore tests | P1 | Admin page component tested only via E2E |
| StoreSettingsStore tests | P1 | Settings CRUD untested at unit level |
| ProfileStore tests | P2 | Profile update/change password untested |
| CatalogPage component | P1 | Product list/grid display component |
| ProductDetail component | P1 | Product detail page component |
| SearchFacets component | P2 | Search facet filtering |

---

## How to Run

```bash
# All frontend tests
cd src/web && pnpm test

# Watch mode
cd src/web && npx ng test

# With coverage
cd src/web && npx ng test --code-coverage
```

---

*Generated from spec files in `src/web/src/app/**/*.spec.ts`.*
