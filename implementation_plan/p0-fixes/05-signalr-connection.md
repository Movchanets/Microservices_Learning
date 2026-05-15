# P0-05 — SignalR Connection

**Goal**: Fix the race condition where SignalR starts before auth completes, and fix server-side broadcast targeting.

**Fixes**: MISSING.md #3.1, #3.2, #3.3

---

## Problem

In `src/web/src/app/app.config.ts` (lines 72-74):
```typescript
void inject(AuthStore).checkAuth();       // fire-and-forget
void inject(NotificationService).start(); // fire-and-forget — races with checkAuth
```

Both run concurrently. `NotificationService.start()` reads `localStorage.getItem('buyerId')` which is never written by AuthStore — so it always gets `'guest-user'`.

Additionally, `NotificationService` doesn't pass a JWT token to the SignalR hub.

---

## Step 1: Fix the race condition in app.config.ts

File: `src/web/src/app/app.config.ts`

Change from parallel fire-and-forget to sequential:
```typescript
provideAppInitializer(() => {
  if (isPlatformBrowser(inject(PLATFORM_ID))) {
    // Auth must complete before SignalR connects (needs buyerId + token)
    void inject(AuthStore).checkAuth().then(() => {
      void inject(NotificationService).start();
    });
  }
}),
```

## Step 2: Update AuthStore to store buyerId

File: `src/web/src/app/core/auth/auth.store.ts`

After successful login/register, store the user ID for SignalR:
```typescript
async login(request: LoginRequest): Promise<void> {
  // ... existing code ...
  const user = await authService.login(request);
  localStorage.setItem('buyerId', user.id);  // ADD THIS
  patchState(store, { user, isAuthenticated: true, isLoading: false });
  // ...
},

async logout(): Promise<void> {
  // ... existing code ...
  localStorage.removeItem('buyerId');  // ADD THIS
  inject(NotificationService).stop();  // ADD THIS
  // ...
},
```

## Step 3: Update NotificationService to use auth token

File: `src/web/src/app/core/signalr/notification.service.ts`

Inject AuthStore and pass the JWT token:
```typescript
import { inject } from '@angular/core';
import { AuthStore } from '../auth/auth.store';

// In start():
const authStore = inject(AuthStore);
const user = authStore.user();
const buyerId = user?.id || localStorage.getItem('buyerId') || 'anonymous';

this.hubConnection = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', {
    headers: { 'x-buyer-id': buyerId },
    // If hub requires JWT:
    // accessTokenFactory: () => authStore.accessToken() || '',
    transport: HttpTransportType.WebSockets,
  })
  // ...
```

## Step 4: Fix broadcast targeting in Notification.Worker

### 4a: Add BuyerId to PaymentFailedEvent

File: `src/BuildingBlocks/SharedContracts/Events/Payment/PaymentFailedEvent.cs`

Add `string BuyerId`:
```csharp
public record PaymentFailedEvent(
    Guid OrderId,
    string BuyerId,
    string Reason,
    DateTime Timestamp);
```

### 4b: Add BuyerId to InventoryReservationFailedEvent

File: `src/BuildingBlocks/SharedContracts/Events/Inventory/InventoryReservationFailedEvent.cs`

Add `string BuyerId`:
```csharp
public record InventoryReservationFailedEvent(
    Guid OrderId,
    string BuyerId,
    string Reason,
    DateTime Timestamp);
```

### 4c: Update Notification consumers

Files in `src/Microservices/Notification/Notification.Worker/Consumers/`:

Replace `Clients.All` with `Clients.User(evt.BuyerId)`:
```csharp
await hubContext.Clients
    .User(evt.BuyerId)  // was: Clients.All
    .SendAsync("OrderUpdate", new OrderUpdate(...));
```

## Step 5: Verify OrderStore has updateOrderStatus

File: `src/web/src/app/features/orders/order.store.ts`

Ensure the method exists (called by NotificationBridgeComponent):
```typescript
updateOrderStatus(orderId: string, status: OrderStatus): void {
  patchState(store, {
    orders: store.orders().map(o =>
      o.id === orderId ? { ...o, status } : o
    ),
  });
}
```

## Verification
- `dotnet build Marketplace.slnx`
- `pnpm nx run web:build`
- Open browser → Network → WS tab → SignalR connects AFTER auth
- buyerId in SignalR header matches logged-in user
- PaymentFailedEvent targets specific user

## Done When
- [ ] Auth completes before SignalR starts (no race)
- [ ] buyerId stored in localStorage on login
- [ ] buyerId removed on logout, SignalR stopped
- [ ] PaymentFailedEvent includes BuyerId
- [ ] InventoryReservationFailedEvent includes BuyerId
- [ ] Notification consumers use Clients.User()
- [ ] All builds pass
