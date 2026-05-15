# P0-01 — Authentication & Authorization

**Goal**: Add JWT Bearer auth to all unprotected backend endpoints and implement frontend route guards.

**Fixes**: MISSING.md #1.1, #1.2, #1.4, #2.6, #4.3

---

## Step 1: Backend — Add auth to Ordering endpoints

File: `src/Microservices/Ordering/Ordering.API/Endpoints/OrderEndpoints.cs`

- Add `.RequireAuthorization()` to the group
- Extract `buyerId` from JWT claim (`ClaimTypes.NameIdentifier` or `sub`) instead of accepting it as a parameter
- Admin-only endpoints (list all orders) get `.RequireAuthorization("Admin")`

```csharp
var group = app.MapGroup("/api/orders")
    .WithTags("Orders")
    .WithOpenApi()
    .RequireAuthorization();
```

## Step 2: Backend — Add auth to Cart endpoints

File: `src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs`

- Add `.RequireAuthorization()` to the group
- Replace `[FromHeader(Name = "x-buyer-id")]` with JWT claim extraction
- Update Program.cs to add JWT Bearer auth (same pattern as other services)

```csharp
// Extract buyerId from JWT instead of header
group.MapGet("/", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
{
    var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(bearerId)) return Results.Unauthorized();
    // ...
});
```

## Step 3: Backend — Add auth to Inventory endpoints

File: `src/Microservices/Inventory/Inventory.API/Endpoints/InventoryEndpoints.cs` (find actual path)

- Add `.RequireAuthorization()` to write endpoints (create, add-stock)
- Read endpoint (get quantity) can stay public

## Step 4: Backend — Add auth to Payment endpoints

File: `src/Microservices/Payment/Payment.API/Endpoints/PaymentEndpoints.cs` (find actual path)

- Add `.RequireAuthorization()` to all endpoints

## Step 5: Backend — Admin role policy on StoreManagement

File: `src/Microservices/StoreManagement/StoreManagement.API/Endpoints/StoreEndpoints.cs`

- Add authorization policy for admin:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Seller", policy => policy.RequireRole("Seller", "Admin"));
});
```
- Apply `.RequireAuthorization("Admin")` to verify endpoint
- Apply `.RequireAuthorization("Seller")` to create/update endpoints

## Step 6: Frontend — Create AuthGuard

File: `src/web/src/app/core/auth/auth.guard.ts`

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

export const authGuard: CanActivateFn = () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (authStore.isAuthenticated()) {
    return true;
  }
  return router.createUrlTree(['/auth/login']);
};
```

## Step 7: Frontend — Create RoleGuard

File: `src/web/src/app/core/auth/role.guard.ts`

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

export const roleGuard = (...roles: string[]): CanActivateFn => {
  return () => {
    const authStore = inject(AuthStore);
    const router = inject(Router);
    const user = authStore.user();

    if (user && roles.includes(user.role)) {
      return true;
    }
    return router.createUrlTree(['/']);
  };
};
```

## Step 8: Frontend — Apply guards to routes

File: `src/web/src/app/app.routes.ts`

```typescript
{
  path: 'orders',
  canActivate: [authGuard],
  loadChildren: () => import('./features/orders/orders.routes'),
},
{
  path: 'seller',
  canActivate: [authGuard, roleGuard('Seller', 'Admin')],
  loadChildren: () => import('./features/seller-dashboard/seller.routes'),
},
{
  path: 'admin',
  canActivate: [authGuard, roleGuard('Admin')],
  loadChildren: () => import('./features/admin/admin.routes'),
},
```

## Verification
- `dotnet build Marketplace.slnx`
- `pnpm nx run web:build`
- Unauthenticated requests to /api/orders return 401
- Non-seller users redirected from /seller
- Non-admin users redirected from /admin

## Done When
- [ ] Ordering, Cart, Inventory, Payment endpoints require auth
- [ ] StoreManagement verify endpoint requires Admin role
- [ ] Frontend AuthGuard + RoleGuard created
- [ ] Guards applied to orders, seller, admin routes
- [ ] All builds pass
