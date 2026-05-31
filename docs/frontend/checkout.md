# Checkout Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/checkout/` |
| **Store Scope** | `CheckoutStore` — `providedIn: 'root'` (singleton) |
| **Route Prefix** | `/checkout` |
| **Guard** | `authGuard` |
| **Render Mode** | `RenderMode.Server` (SSR) |

## Component Structure

```
checkout/
├── checkout.store.ts              # CheckoutStore (root singleton)
├── checkout.models.ts             # Order, Address, OrderStatus types
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

**Key methods:** `setAddress(address)`, `setShippingMethod(method)`, `submitCheckout()`, `setOrder(order)`, `setPollingExpired(expired)`, `markTerminalFailure(reason)`, `retryCheckout()`, `reset()`

## Checkout Flow

1. User fills `AddressFormComponent` → `setAddress()`
2. Selects shipping method → `setShippingMethod()`
3. Clicks "Place Order" → `submitCheckout()`
   - Validates cart not empty and address exists
   - Calls `CartStore.checkout(address)` → POST `/api/cart/checkout`
   - Backend publishes `OrderSubmittedEvent` → saga orchestrator
   - Sets optimistic order with `status: 'Submitted'`
4. `CheckoutStatusComponent` polls or receives SignalR updates
   - On terminal failure: `markTerminalFailure(reason)` → show retry
   - On polling timeout: `setPollingExpired(true)` → show retry
5. On success: redirects to order detail page

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
- **`checkout-status` component:** Appears to handle polling but the polling logic is not in the store — likely lives in the component itself.
- **Optimistic order lacks items:** The optimistic `Order` object has `items: []` — real items arrive via SignalR/polling update.
- **No address validation:** `AddressFormComponent` uses reactive forms but no server-side address validation.
- **Shipping method:** Only `standard`/`express` toggle — no carrier selection, delivery date estimation, or shipping cost calculation on frontend.
- **`retryCheckout()` resets state** but doesn't clear the CartStore's `checkoutCorrelationId` — potential stale state.
