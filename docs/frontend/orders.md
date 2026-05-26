# Orders Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/orders/` |
| **Store Scope** | `OrderStore` — `providedIn: 'root'` (singleton) |
| **Route Prefix** | `/orders` |
| **Guard** | `authGuard` |
| **Render Mode** | `RenderMode.Server` (SSR) |

## Component Structure

```
orders/
├── order.store.ts                # OrderStore (root singleton)
├── order.service.ts              # HTTP service → BFF /api/orders
├── order.service.spec.ts         # ✅ Tests
├── orders.routes.ts              # Default export routes
├── order-list/
│   ├── order-list.ts             # OrderListComponent — order history table
│   └── order-list.spec.ts        # ✅ Tests
├── order-detail/
│   ├── order-detail.ts           # OrderDetailComponent — single order view
│   └── order-detail.spec.ts      # ✅ Tests
├── order-timeline/
│   ├── order-timeline.ts         # OrderTimelineComponent — status timeline
│   └── order-timeline.spec.ts    # ✅ Tests
└── components/
    └── status-badge/
        ├── status-badge.ts       # StatusBadgeComponent — colored status chip
        └── status-badge.spec.ts  # ✅ Tests
```

## SignalStore State Management

### OrderStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `orders` | `Order[]` | All orders for current user |
| `selectedOrder` | `Order \| null` | Currently viewed order |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |

**Computed signals:** `completedOrders`, `activeOrders`, `hasOrders`

**Key methods:** `loadOrders(buyerId)`, `loadOrderById(orderId)`, `updateOrderStatus(orderId, status)`, `cancelOrder(orderId, reason?)`, `clearSelected()`

## Order Status Lifecycle

```
Submitted → InventoryReserved → PaymentProcessing → Completed
                                                  → Cancelled
                                                  → Faulted
```

`updateOrderStatus()` is called by `NotificationService` (SignalR) when real-time status updates arrive. It guards against no-op updates to avoid unnecessary signal re-fires.

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/orders` | `OrderListComponent` | `authGuard` |
| `/orders/:id` | `OrderDetailComponent` | `authGuard` |

> **Note:** `OrderListComponent` is also reused in `/profile/orders` via `ProfileRoutes`.

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| `order.service.spec.ts` | ✅ | Passing |
| `order.store.spec.ts` | ✅ | Passing |
| `order-list/order-list.spec.ts` | ✅ | Passing |
| `order-detail/order-detail.spec.ts` | ✅ | Passing |
| `order-timeline/order-timeline.spec.ts` | ✅ | Passing |
| `components/status-badge/status-badge.spec.ts` | ✅ | Passing |

**E2E Coverage:** Minimal — `order-history.spec.ts` (~3 tests). Missing: cancellation, saga flow, status timeline, re-order.

## Known Gaps / Issues

- **No re-order functionality:** Users cannot re-order a previous order (add all items back to cart).
- **Cancellation flow:** `cancelOrder()` exists in store and service but the UI for triggering it (with reason input) is not visible in the component tree.
- **No order search/filter:** `OrderListComponent` loads all orders — no date range, status filter, or search.
- **Profile route reuse:** `/profile/orders` reuses `OrderListComponent` but `loadOrders(buyerId)` requires the buyer ID — the component must get it from `AuthStore`.
- **SignalR integration:** `updateOrderStatus()` is designed for real-time updates via `NotificationService`, but the bridge between SignalR events and this method lives in core, not in this feature.
