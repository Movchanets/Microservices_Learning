# Frontend State — 2026-05-17

## Overview

Angular 19+ SPA with NgRx SignalStore, standalone components, lazy-loaded routes. All API calls go through YARP BFF (withCredentials: true).

**Tests:** 293 Vitest tests across 36 spec files — all passing.

---

## Core Infrastructure ✅ Implemented

| Component | Status |
|-----------|--------|
| app.routes.ts | ✅ All routes configured with guards |
| app.config.ts | ✅ HttpClient, interceptors, category tree init, auth init (no direct SignalR start) |
| auth.guard.ts | ✅ Working |
| role.guard.ts | ✅ Working |
| api.interceptor.ts | ✅ withCredentials: true |
| error.interceptor.ts | ✅ Toast service integration |
| notification.service.ts | ✅ SignalR with query string buyerId, auto-reconnect |
| AuthStore | ✅ checkAuth, login, register, logout — starts/stops SignalR lifecycle |
| ToastService | ✅ Error/success notifications |
| CategoryTreeService | ✅ Loaded on app init, cached in signal |

**Changes since 2026-05-16:**
- SignalR start removed from `app.config.ts` initializer
- `AuthStore` now manages SignalR lifecycle:
  - `login()` → starts SignalR after user fetched
  - `register()` → starts SignalR after user fetched
  - `checkAuth()` → starts SignalR if session valid
  - `logout()` → stops SignalR
- `NotificationService.start(buyerId)` uses query string (`?buyerId=`) instead of custom header
- `NotificationService.stop()` properly cleans up hub connection

---

## Feature Modules

### 1. Catalog ✅ Implemented

| Component | Status |
|-----------|--------|
| ProductListComponent | ✅ Grid with pagination, category filter |
| ProductDetailComponent | ✅ Full product view |
| CatalogStore | ✅ Load, filter, search |
| CatalogService | ✅ API calls working |

**TODOs (6):**
- ❌ No "Add to Cart" button integration on product detail
- ❌ No InventoryService for stock availability checks
- ❌ No "Sticky Buy Box" (pinned add-to-cart when scrolling)
- ❌ No "Frequently Bought Together" section
- ❌ No product variant selector (color, size)
- ❌ No Community Q&A and Reviews section

---

### 2. Cart ✅ Implemented

| Component | Status |
|-----------|--------|
| CartComponent | ✅ Item list, quantity controls |
| CartStore | ✅ loadCart, addToCart, updateQuantity, removeFromCart, checkout |
| CartService | ✅ API calls working |

**TODOs (2):**
- ❌ No slide-out cart drawer (future design exists)
- ❌ addToCart uses full cart replacement (optimistic update with fallback)

**Changes since 2026-05-16:**
- ✅ x-buyer-id header pattern removed (now uses JWT claims only via BFF)

---

### 3. Checkout ✅ Implemented

| Component | Status |
|-----------|--------|
| CheckoutPageComponent | ✅ Cart summary, submit |
| CheckoutStore | ✅ submitCheckout, setOrder |
| OrderTimelineComponent | ✅ Step visualization |

**TODOs (3):**
- ❌ No address form (MISSING.md #2.2, #5.7)
- ❌ No payment method selection
- ❌ No express checkout options (Apple Pay, Google Pay)
- ❌ No free shipping progress bar

---

### 4. Orders ✅ Implemented

| Component | Status |
|-----------|--------|
| OrderListComponent | ✅ Buyer order history |
| OrderDetailComponent | ✅ Order details with timeline |
| OrderStore | ✅ loadOrders, loadOrderById |
| OrderService | ✅ API calls working |

**TODOs:**
- ❌ No order cancellation UI

---

### 5. Seller Dashboard ✅ Implemented

| Component | Status |
|-----------|--------|
| SellerDashboardComponent | ✅ Overview with tabs |
| SellerProductListComponent | ✅ Product CRUD |
| SellerProductFormComponent | ✅ Create/edit form |
| StoreSettingsComponent | ✅ Store name, description |
| SellerOrdersComponent | ✅ Seller order view |
| SellerProductStore | ✅ Full CRUD |
| StoreSettingsStore | ✅ Load, create, update |
| SellerProductService | ✅ API calls working |
| StoreService | ✅ API calls working |

**TODOs:**
- ❌ No inventory management UI (MISSING.md #5.2)
- ❌ No media upload integration in product form
- ❌ Sales summary returns hardcoded zeros (needs Ordering.API endpoint)
- ⚠️ SellerOrdersComponent bypasses store pattern (direct HttpClient injection)

---

### 6. Admin Panel ✅ Implemented

| Component | Status |
|-----------|--------|
| AdminComponent | ✅ Tabs: Users, Stores, Pending |
| AdminStore | ✅ Full CRUD for users/stores |
| AdminUserService | ✅ API calls working |
| AdminStoreService | ✅ API calls working |

**TODOs:**
- ❌ No bulk operations

---

### 7. Auth & Profile ✅ Implemented

| Component | Status |
|-----------|--------|
| LoginComponent | ✅ Working |
| RegisterComponent | ✅ Working |
| ForgotPasswordComponent | ✅ Placeholder UI |
| ProfileComponent | ✅ Display user info |

**TODOs (5):**
- ❌ No profile edit form (MISSING.md #1.3)
- ❌ No change password UI (MISSING.md #6.6)
- ❌ No order history tab in profile
- ❌ No notification badges on sidebar tabs
- ❌ Not transformed into full "Personal Account" hub (sidebar nav)

---

## Route Guards

| Route | Guard | Status |
|-------|-------|--------|
| /catalog | None (public) | ✅ |
| /cart | authGuard | ✅ |
| /checkout | authGuard | ✅ |
| /orders | authGuard | ✅ |
| /seller | authGuard + roleGuard('Seller', 'Admin') | ✅ |
| /admin | authGuard + roleGuard('Admin') | ✅ |
| /profile | authGuard | ✅ |
| /** | NotFoundComponent | ✅ |

---

## State Management Pattern

All stores follow NgRx SignalStore pattern:
- `withState<T>()` — typed state interface
- `withComputed()` — derived signals
- `withMethods()` — async operations with loading/error handling
- `withHooks()` — onInit for auto-loading

**Pattern compliance:** ✅ All stores follow AGENTS.md guidelines

---

## UI Components

| Library | Usage |
|---------|-------|
| Spartan/UI | ✅ Used for UI primitives |
| Tailwind CSS | ✅ Styling |
| Lucide Angular | ✅ Icons (26+ icons registered) |
| Angular CDK | ✅ Virtual scroll ready |

---

## Performance

| Pattern | Status |
|---------|--------|
| Lazy loading | ✅ All feature routes |
| OnPush change detection | ✅ All components |
| Standalone components | ✅ All components |
| New control flow | ✅ @if, @for, @switch |
| Signals for state | ✅ All stores |
| No NgModules | ✅ Clean |

---

## Test Coverage — 293 tests, 36 spec files

| Spec File | Tests | Coverage |
|-----------|-------|---------|
| header.spec.ts | 21 | Mega-menu, search, cart badge, user menu, Admin Panel |
| seller-orders.spec.ts | 22 | Order loading, status update, getNextStatus, statusClass |
| order-detail.spec.ts | 19 | Loading/error, cancel button, canCancel, confirmCancel |
| inventory-list.spec.ts | 19 | Filtering, status labels/classes, getCount, confirmAddStock |
| order.store.spec.ts | 16 | Load, cancel, selectedOrder update |
| inventory.store.spec.ts | 14 | Load inventory, stock status, lowStockItems, addStock |
| mega-menu.spec.ts | 9 | Root categories, subcategory display, navigation |
| frequently-bought-together.spec.ts | 9 | Product grouping, add all to cart |
| category-tree.service.spec.ts | 9 | HTTP endpoint, success/error, loading state |
| catalog.store.spec.ts | 8 | Products, filters, search |
| order.service.spec.ts | 9 | CRUD, cancelOrder, updateOrderStatus |
| cart.store.spec.ts | 8 | Add, update, remove, checkout |
| login.spec.ts | 7 | Form validation, submit, error handling |
| register.spec.ts | 7 | Form validation, submit, error handling |
| seller-product.store.spec.ts | 7 | CRUD operations |
| cart-page.spec.ts | 6 | Display, empty state, checkout |
| toast-container.spec.ts | 6 | Show/hide, auto-dismiss |
| seller-product.service.spec.ts | 6 | HTTP calls |
| auth.service.spec.ts | 6 | Login, register, logout, getUser |
| cart.service.spec.ts | 6 | HTTP calls |
| status-badge.spec.ts | 12 | All status variants |
| notification.service.spec.ts | 4 | Start, stop, message handling |
| auth.store.spec.ts | 4 | Login, register, checkAuth |
| auth.interceptor.spec.ts | 3 | withCredentials, error handling |
| theme.service.spec.ts | 3 | Dark/light mode |
| language.service.spec.ts | 3 | i18n |
| api.interceptor.spec.ts | 3 | withCredentials |
| mini-cart.spec.ts | 3 | Display, remove, total |
| order-list.spec.ts | 2 | Display, empty state |
| checkout-page.spec.ts | 3 | Display, submit |
| not-found.spec.ts | 4 | 404 page |
| app.spec.ts | 1 | Root component |
| inventory.service.spec.ts | 4 | HTTP calls |
