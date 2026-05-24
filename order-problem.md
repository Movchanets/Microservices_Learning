# Order Checkout UI Blocking — Problem Summary

## Symptom

After clicking "Place Order" on the `/checkout` route, the UI freezes on the
"Order Submitted" loading spinner. The entire page becomes unresponsive.
The backend saga completes successfully (all 5 status transitions fire),
but the frontend never reacts to the SignalR updates.

## Root Cause (Confirmed via Logs)

**Angular `effect()` loses signal dependency tracking** when reading multiple
signals where one fires first and the other fires later.

### The Failing Code (before fix)

```typescript
effect(() => {
  const update = this.notifications.orderUpdates();  // Signal A
  const submitted = this.checkoutStore.submitted();   // Signal B
  const hasOrder = this.checkoutStore.hasOrder();      // Signal C
  const currentOrder = this.checkoutStore.order();     // Signal D

  if (update && submitted) {
    if (!hasOrder) {
      this.checkoutStore.setOrder({ ... });  // Changes Signal C + D
    } else {
      this.checkoutStore.setOrder({ ... });  // Changes Signal D → INFINITE LOOP
    }
  }
});
```

### Two Distinct Bugs

#### Bug 1: Effect dies after first run

The effect fires once when `submitted` changes to `true` (Signal B).
At that point, `orderUpdates()` is `null` (Signal A hasn't fired yet).

When SignalR later delivers a message and `orderUpdates()` changes,
**the effect never re-fires**. Angular's scheduler dropped the dependency.

**Evidence from console:**
```
[Checkout] effect fired {update: null, submitted: true}   ← ran once
[SignalR] OrderUpdate received {status: 'Submitted'}      ← SignalR delivers
[SignalR] orderUpdates signal set                          ← signal updated
                                                           ← NO MORE EFFECT FIRES
```

The backend sends 5 status updates (Submitted → InventoryReserved →
PaymentProcessing → Completed). The frontend receives ALL of them via
SignalR. The effect fires for NONE of them.

#### Bug 2: Infinite loop (pre-existing)

When the effect DID fire (from `hasOrder`/`order` signal changes),
the `else` branch called `setOrder({ ...currentOrder, status })` which
creates a **new object reference** every time. This changes `order()`,
which triggers the effect again, which creates another new object, etc.

**This was the "entire page unresponsive" symptom** — a synchronous
infinite loop blocking the browser's main thread.

### Why Polling Didn't Save Us

Polling starts after the HTTP POST succeeds and does fetch the order.
But when polling calls `setOrder()`, it triggers the same infinite loop
in the effect (Bug 2). So polling made things worse, not better.

## The Fix

### Step 1: Prevent infinite loop (line 73)

```typescript
// Before — always creates new object:
if (currentOrder && currentOrder.id === update.orderId) {

// After — only when status actually changed:
if (currentOrder && currentOrder.id === update.orderId && currentOrder.status !== update.status) {
```

### Step 2: Fix effect dependency tracking

Replace direct `effect()` with `computed()` + `effect()` pattern:

```typescript
private handleSignalRUpdate(): void {
  // Combine both trigger signals into ONE computed signal
  const trigger = computed(() => {
    const update = this.notifications.orderUpdates();
    const submitted = this.checkoutStore.submitted();
    return update && submitted ? update : null;
  });

  // Effect on the single computed signal — always tracks properly
  effect(() => {
    const update = trigger();  // Single dependency
    if (!update) return;

    const hasOrder = this.checkoutStore.hasOrder();
    const currentOrder = this.checkoutStore.order();

    if (!hasOrder) {
      this.checkoutStore.setOrder({ ... });
      this.stopPolling();
    } else {
      if (currentOrder.status !== update.status) {
        this.checkoutStore.setOrder({ ...currentOrder, status: update.status });
      }
    }

    if (TERMINAL_FAILURE_STATUSES.includes(update.status)) {
      this.checkoutStore.markTerminalFailure(update.reason);
    }
  });
}
```

### Step 3: Fix retry deadlock

`retryCheckout()` didn't clear `order`, so `hasOrder()` stayed `true`
and the template showed the old status instead of the form:

```typescript
retryCheckout(): void {
  patchState(store, {
    submitted: false,
    pollingExpired: false,
    error: null,
    order: null,        // ← was missing
    submitting: false,  // ← was missing
  });
}
```

## Files Changed

| File | Changes |
|------|---------|
| `checkout-page.ts` | `computed()` + `effect()` pattern, `handleSignalRUpdate()`, diagnostic logging |
| `checkout.store.ts` | `markTerminalFailure()`, fixed `retryCheckout()` to clear `order` |
| `checkout-status.ts` | `error` input, `retry` output, `Processing` case, `@default` case, "Try Again" buttons |
| `checkout-page.html` | Wired `[error]` and `(retry)` bindings |
| `notification.service.ts` | Diagnostic logging for connection and message receipt |

## Backend Context

The backend ordering flow is a MassTransit saga (`OrderStateMachine`):

```
OrderSubmitted → ReserveInventory → ProcessPayment → Completed/Cancelled
```

Each transition publishes `OrderStatusChangedEvent` → Notification worker
consumes it → pushes `OrderUpdate` via SignalR hub → frontend receives it.

The saga completes in <500ms. SignalR delivers all 5 updates within ~12s.
The frontend was receiving them but the Angular effect wasn't processing them.

## Lessons Learned

1. **Angular effects can silently drop signal dependencies** when reading
   multiple signals where one fires before the other. Use `computed()` to
   combine trigger signals into a single dependency.

2. **`setOrder({ ...currentOrder, status })` creates a new object every time**
   even if the status is the same. Always guard with `status !== update.status`
   to prevent infinite loops in reactive pipelines.

3. **`retryCheckout()` must clear ALL state** — not just `submitted` and `error`.
   If `order` persists, `hasOrder()` stays `true` and the template shows the
   old status instead of the form.

4. **Diagnostic logging is essential** for debugging reactive state issues.
   The `[Checkout]` and `[SignalR]` prefixed logs made the exact failure
   point immediately visible in the console.

## Status

- [x] Infinite loop fixed (status guard on setOrder)
- [x] Effect dependency tracking fixed (computed + effect pattern)
- [x] Retry deadlock fixed (retryCheckout clears order)
- [x] Terminal failure handling (markTerminalFailure + checkout-status UI)
- [x] Diagnostic logging added (can be removed after verification)
- [ ] Verify fix in running application
