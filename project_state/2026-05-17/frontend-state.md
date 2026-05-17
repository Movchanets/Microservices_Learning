# Frontend State — 2026-05-17

## Overview

Angular 19+ SPA with NgRx SignalStore, standalone components, lazy-loaded routes. All API calls go through YARP BFF (withCredentials: true).

---

## Core Infrastructure ✅ Implemented

| Component | Status |
|-----------|--------|
| app.routes.ts | ✅ All routes configured with guards |
| app.config.ts | ✅ HttpClient, interceptors, SignalR init |
| auth.guard.ts | ✅ Working |
| role.guard.ts | ✅ Working |
| api.interceptor.ts | ✅ withCredentials: true |
| error.interceptor.ts | ✅ Toast service integration |
| notification.service.ts | ✅ SignalR with auto-reconnect |
| AuthStore | ✅ checkAuth, login, register, logout |
| ToastService | ✅ Error/success notifications |

---

## Feature Modules

### 1. Catalog ✅ Implemented

| Component | Status |
|-----------|--------|
| ProductListComponent | ✅ Grid with pagination, category filter |
| ProductDetailComponent | ✅ Full product view |
| CatalogStore | ✅ Load, filter, search |
| CatalogService | ✅ API calls working |

**Plan 10 Changes:**
- ✅ BuyBoxComponent now has `sellerId` input (optional)
- ✅ product-detail.ts passes `p.sellerId` to buy-box component
- ✅ BuyBoxComponent.onAddToCart and onBuyNow pass sellerId to cart store

**TODOs:**
- ❌ No InventoryService for stock availability checks
- ❌ No "Sticky Buy Box" (pinned add-to-cart when scrolling)
- ❌ No product variant selector (color, size)
- ❌ No Community Q&A and Reviews section

---

### 2. Cart ✅ Implemented

| Component | Status |
|-----------|--------|
| CartComponent | ✅ Item list, quantity controls |
| CartStore | ✅ loadCart, addToCart, updateQuantity, removeFromCart, checkout |
| CartService | ✅ API calls working |

**Plan 10 Changes:**
- ✅ CartService.addItem now accepts optional `sellerId` parameter
- ✅ CartStore.addToCart passes sellerId to service
- ✅ CartItem interface (cart.models.ts) includes `sellerId?: string`

**TODOs:**
- ❌ No slide-out cart drawer (future design exists)
- ❌ addToCart uses full cart replacement (optimistic update with fallback)
- ❌ x-buyer-id header pattern still in code (should use JWT claims only)

---

### 3. Checkout ✅ Implemented

| Component | Status |
|-----------|--------|
| CheckoutPageComponent | ✅ Cart summary, submit |
| CheckoutStore | ✅ submitCheckout, setOrder |
| OrderTimelineComponent | ✅ Step visualization |

**TODOs:**
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

**TODOs:**
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
| Lucide Angular | ✅ Icons (26 icons registered) |
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
