# Cart Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/cart/` |
| **Store Scope** | `CartStore` — `providedIn: 'root'` (singleton) |
| **Route Prefix** | `/cart` |
| **Guard** | `authGuard` |
| **Render Mode** | `RenderMode.Server` (SSR) |

## Component Structure

```
cart/
├── cart.store.ts              # CartStore (root singleton)
├── cart.service.ts            # HTTP service → BFF /api/cart
├── cart.models.ts             # CartItemDetails type
├── cart.routes.ts             # Default export routes
├── cart-page/
│   ├── cart-page.ts           # CartPageComponent — full cart page
│   ├── cart-page.spec.ts      # ✅ Tests
│   └── mini-cart (in shared)  # MiniCartComponent — inline cart preview
└── components/
    └── mini-cart/
        ├── mini-cart.ts       # MiniCartComponent — compact item list
        └── mini-cart.spec.ts  # ✅ Tests
```

> **Note:** The `cart-drawer` component lives in `shared/components/cart-drawer/` (global overlay), not under this feature.

## SignalStore State Management

### CartStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `items` | `CartItemDetails[]` | Cart items with product details |
| `cartId` | `string \| null` | Anonymous cart identifier |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |
| `checkoutCorrelationId` | `string \| null` | Saga correlation ID after checkout |
| `isDrawerOpen` | `boolean` | Cart drawer visibility |

**Computed signals:** `totalItems`, `isEmpty`, `totalPrice`

**Key methods:** `loadCart()`, `addToCart(productId, quantity)`, `updateQuantity(productId, quantity)`, `removeFromCart(productId)`, `checkout(address?)`, `showDrawer()`, `hideDrawer()`, `toggleDrawer()`, `clearAnonymousCart()`, `refreshAfterLogin()`

**Hooks:** `onInit` — calls `loadCart()` in browser only (`isPlatformBrowser` check)

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/cart` | `CartPageComponent` | `authGuard` |

## Anonymous Cart Flow

1. Anonymous user adds item → `CartService.addItem()` returns `cartId`
2. `cartId` persisted in `localStorage` via `CartService.setCartId()`
3. Subsequent requests include `X-Cart-Id` header
4. On login, `refreshAfterLogin()` → backend merges anonymous cart → response has `buyerId` → `clearCartId()` removes from `localStorage`

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| `cart-page/cart-page.spec.ts` | ✅ | Passing |
| `components/mini-cart/mini-cart.spec.ts` | ✅ | Passing |
| `cart.store.spec.ts` | ✅ | Passing |
| `cart.service.spec.ts` | ✅ | Passing |

**E2E Coverage:** **Not covered** — all E2E specs deleted (add-to-cart, cart-drawer — 8 tests). P0 priority to re-add.

## Known Gaps / Issues

- **Zero E2E coverage:** Critical revenue path gap. Cart add/remove/update, anonymous cart, cart merge all untested at E2E level.
- **Drawer state in CartStore:** `isDrawerOpen` is UI state mixed into domain store — could be separated.
- **No optimistic UI:** Every mutation (`addToCart`, `updateQuantity`, `removeFromCart`) triggers a full `loadCart()` re-fetch. No local optimistic update.
- **Checkout address:** `checkout()` accepts an optional address object but the BFF may not require it — the interface is loosely defined.
- **Cart page requires auth:** Route guarded by `authGuard`, but anonymous carts are supported at the API level. Anonymous users can't view cart page.
