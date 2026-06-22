# Cart Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/cart/` |
| **Store Scope** | `CartStore` — `providedIn: 'root'` (singleton) |
| **Route Prefix** | `/cart` |
| **Guard** | `authGuard` |
| **Render Mode** | `RenderMode.Server` (SSR) |
| **Last Updated** | 2026-06-19 |

## Component Structure

```
cart/
├── cart.store.ts              # CartStore (root singleton)
├── cart.store.spec.ts         # ✅ Tests
├── cart.service.ts            # HTTP service → BFF /bff/cart + /api/cart
├── cart.service.spec.ts       # ✅ Tests
├── cart.models.ts             # CartItemDetails, ShoppingCart, CheckoutResponse
├── cart.routes.ts             # Default export routes
├── cart-page/
│   ├── cart-page.ts           # CartPageComponent — full cart page
│   └── cart-page.spec.ts      # ✅ Tests
└── components/
    └── mini-cart/
        ├── mini-cart.ts       # MiniCartComponent — compact item list (header badge)
        └── mini-cart.spec.ts  # ✅ Tests
```

## Models (`cart.models.ts`)

| Type | Fields | Description |
|:---|:---|:---|
| `CartItemDetails` | `productId`, `skuId`, `skuCode`, `title`, `imageUrl \| null`, `quantity`, `price`, `lineTotal`, `storeId` | Single cart item with product details |
| `ShoppingCart` | `buyerId \| null`, `cartId`, `items: CartItemDetails[]`, `totalPrice`, `totalItems` | Full cart response from API |
| `CheckoutResponse` | `correlationId` | Checkout submission response |

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

**Key methods:**

| Method | Signature | Description |
|:---|:---|:---|
| `loadCart()` | `async () => Promise<void>` | Fetch cart from BFF, handle anonymous/auth merge |
| `addToCart()` | `async (productId, skuId, skuCode, quantity) => Promise<void>` | Add item, re-fetch enriched cart, open drawer |
| `updateQuantity()` | `async (skuId, quantity) => Promise<void>` | Update qty; delegates to `removeFromCart` if ≤ 0 |
| `removeFromCart()` | `async (skuId) => Promise<void>` | Remove item by skuId |
| `checkout()` | `async (address?) => Promise<void>` | POST checkout, clear cart, set correlationId |
| `showDrawer()` | `() => void` | Open cart drawer |
| `hideDrawer()` | `() => void` | Close cart drawer |
| `toggleDrawer()` | `() => void` | Toggle drawer visibility |
| `clearAnonymousCart()` | `() => void` | Clear localStorage cartId + reset items |
| `refreshAfterLogin()` | `async () => Promise<void>` | Re-load cart after login (triggers server-side merge) |

**Hooks:** `onInit` — calls `loadCart()` in browser only (`isPlatformBrowser` check)

## CartService (`cart.service.ts`)

| Method | HTTP | Endpoint | Returns |
|:---|:---|:---|:---|
| `getCart()` | GET | `/bff/cart` | `ShoppingCart` |
| `addItem()` | POST | `/api/cart/items` | `ShoppingCart` |
| `updateItem()` | PUT | `/api/cart/items/{skuId}` | `ShoppingCart` |
| `removeItem()` | DELETE | `/api/cart/items/{skuId}` | `ShoppingCart` |
| `checkout()` | POST | `/api/cart/checkout` | `CheckoutResponse` |
| `deleteCart()` | DELETE | `/api/cart` | `void` |

> **Note:** `getCart()` uses the `/bff/cart` endpoint (enriched DTO with product details). Mutation endpoints (`add`, `update`, `remove`, `checkout`) use `/api/cart` (YARP proxied). All requests include `X-Cart-Id` header for anonymous cart identification.

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/cart` | `CartPageComponent` | `authGuard` |

## Anonymous Cart Flow

1. Anonymous user adds item → `CartService.addItem()` returns `ShoppingCart` with `cartId`
2. `cartId` persisted in `localStorage` via `CartService.setCartId()`
3. Subsequent requests include `X-Cart-Id` header
4. On login, `refreshAfterLogin()` → `loadCart()` → backend merges anonymous cart → response has `buyerId` → `clearCartId()` removes from `localStorage`

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
