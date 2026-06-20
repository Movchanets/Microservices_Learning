# Checkout Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/checkout/` |
| **Store Scope** | `CheckoutStore` — `providedIn: 'root'` (singleton) |
| **Route Prefix** | `/checkout` |
| **Guard** | `authGuard` |
| **Render Mode** | `RenderMode.Server` (SSR) |
| **Last Updated** | 2026-06-19 |

## Component Structure

```
checkout/
├── checkout.store.ts              # CheckoutStore (root singleton)
├── checkout.store.spec.ts         # ✅ Tests
├── checkout.models.ts             # Order, Address, OrderItem, OrderStatus, PaymentStatus
├── checkout.routes.ts             # Default export routes
├── checkout-page/
│   ├── checkout-page.ts           # CheckoutPageComponent — main checkout flow
│   ├── checkout-page.html         # External template
│   ├── checkout-page.css
│   └── checkout-page.spec.ts      # ✅ Tests
├── checkout-summary/
│   └── checkout-summary.ts        # CheckoutSummaryComponent — order summary
├── checkout-status/
│   └── checkout-status.ts         # CheckoutStatusComponent — polling/status display
└── address-form/
    ├── address-form.ts            # AddressFormComponent — reactive form
    ├── address-form.html
    └── address-form.css
```

## Models (`checkout.models.ts`)

| Type | Fields | Description |
|:---|:---|:---|
| `OrderStatus` | `'Submitted' \| 'InventoryReserved' \| 'PaymentProcessing' \| 'Completed' \| 'Cancelled' \| 'Faulted' \| 'Processing' \| 'Shipped' \| 'Delivered'` | Union type for order lifecycle |
| `OrderItem` | `id`, `sku`, `productName`, `unitPrice`, `quantity`, `totalPrice` | Single line item in an order |
| `Address` | `addressLine1`, `addressLine2?`, `city`, `state`, `postalCode`, `country` | Shipping address |
| `Order` | `id`, `buyerId`, `status: OrderStatus`, `totalAmount`, `createdAt`, `completedAt \| null`, `items: OrderItem[]` | Full order object |
| `PaymentStatus` | `id`, `orderId`, `amount`, `status`, `transactionId \| null`, `failureReason \| null`, `createdAt`, `processedAt \| null` | Payment transaction status |

## SignalStore State Management

### CheckoutStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `address` | `Address \| null` | Shipping address |
| `shippingMethod` | `'standard' \| 'express'` | Selected shipping method |
| `submitting` | `boolean` | Checkout submission in progress |
| `error` | `string \| null` | Error message |
| `order` | `Order \| null` | Optimistic order after submission |
| `submitted` | `boolean` | Whether checkout has been submitted |
| `pollingExpired` | `boolean` | Status polling timed out |

**Computed signals:** `hasOrder`, `orderStatus`

**Key methods:**

| Method | Signature | Description |
|:---|:---|:---|
| `setAddress()` | `(address: Address) => void` | Store shipping address |
| `setShippingMethod()` | `(method: 'standard' \| 'express') => void` | Set shipping method |
| `submitCheckout()` | `async () => Promise<void>` | Validate, call CartStore.checkout(), set optimistic order |
| `setOrder()` | `(order: Order) => void` | Update order (from SignalR/polling) |
| `setPollingExpired()` | `(expired: boolean) => void` | Mark polling timeout |
| `markTerminalFailure()` | `(reason: string \| null) => void` | Handle terminal failure (Cancelled/Faulted) |
| `retryCheckout()` | `() => void` | Reset for retry |
| `reset()` | `() => void` | Full state reset |

## Checkout Flow

1. User fills `AddressFormComponent` → `setAddress()`
2. Selects shipping method → `setShippingMethod()`
3. Clicks "Place Order" → `submitCheckout()`
   - Validates cart not empty and address exists
   - Calls `CartStore.checkout(address)` → POST `/api/cart/checkout`
   - Backend publishes `OrderSubmittedEvent` → saga orchestrator
   - Sets optimistic order with `status: 'Submitted'`
4. `CheckoutPageComponent` starts status polling (2s interval, 60s max)
   - Also listens for SignalR `orderUpdates` for instant transitions
   - On terminal failure: `markTerminalFailure(reason)` → show retry
   - On polling timeout: `setPollingExpired(true)` → show retry
5. On success: "View Orders" link shown

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/checkout` | `CheckoutPageComponent` | `authGuard` |

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| `checkout-page/checkout-page.spec.ts` | ✅ | Passing |
| `checkout.store.spec.ts` | ✅ | Passing |

**E2E Coverage:** Minimal — `checkout-flow.spec.ts` (~2 tests). Missing: payment, edge cases, cart merge, confirmation page.

## Known Gaps / Issues

- **No dedicated payment component:** Payment processing happens server-side via saga. Frontend only shows status — no Stripe/payment form integration visible.
- **Polling lives in component:** `CheckoutPageComponent` owns the polling logic (not the store) — `startStatusPolling()`, `stopPolling()`, `restartPollingWithId()`.
- **Optimistic order lacks items:** The optimistic `Order` object has `items: []` — real items arrive via SignalR/polling update.
- **No address validation:** `AddressFormComponent` uses reactive forms but no server-side address validation.
- **Shipping method:** Only `standard`/`express` toggle — no carrier selection, delivery date estimation, or shipping cost calculation on frontend.
- **`retryCheckout()` resets state** but doesn't clear the CartStore's `checkoutCorrelationId` — potential stale state.
