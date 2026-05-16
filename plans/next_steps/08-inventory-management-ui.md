# Plan 08: Seller Inventory Management UI

## Goal
Add inventory management UI for sellers to view stock levels, add stock, and get low-stock alerts.

## Context
- **Current state:** Inventory.API has endpoints (POST /items, POST /items/{sku}/add-stock, GET /items/{sku}, GET /items). No frontend UI for sellers.
- **Target state:** Seller dashboard with inventory tab showing stock levels, low-stock warnings, and stock adjustment forms.
- **Backend gaps:** No seller-specific inventory endpoint (MISSING.md #5.2)
- **Frontend gaps:** No inventory management UI (MISSING.md #5.2)

## Prerequisites
- Inventory.API has GET /api/inventory/items — exists (auth required)
- Inventory.API has POST /api/inventory/items/{sku}/add-stock — exists
- Seller dashboard has tabs (Products, Orders, Settings) — exists

## Backend Changes

### 1. Add Seller Inventory Endpoint
**File:** `src/Microservices/Inventory/Inventory.API/Endpoints/InventoryEndpoints.cs`

Add endpoint to get inventory for a seller's products:
```csharp
group.MapGet("/items/seller/{sellerId}", async (
    string sellerId,
    [FromServices] IInventoryItemRepository repository,
    CancellationToken ct) =>
{
    var items = await repository.GetBySellerIdAsync(sellerId, ct);
    return Results.Ok(items);
})
.RequireAuthorization("Seller");
```

**New files:**
- `Inventory.Domain/Aggregates/InventoryItem.cs` — add SellerId property
- `Inventory.Infrastructure/Persistence/IInventoryItemRepository.cs` — add GetBySellerIdAsync

### 2. Add Low Stock Threshold
**File:** `src/Microservices/Inventory/Inventory.Domain/Aggregates/InventoryItem.cs`

Add `LowStockThreshold` property (default 5):
```csharp
public int LowStockThreshold { get; private set; } = 5;
public bool IsLowStock => Quantity <= LowStockThreshold && Quantity > 0;
public bool IsOutOfStock => Quantity == 0;
```

### 3. Add Stock Adjustment Endpoint
**File:** `src/Microservices/Inventory/Inventory.API/Endpoints/InventoryEndpoints.cs`

```csharp
group.MapPost("/items/{sku}/adjust", async (
    string sku,
    [FromBody] AdjustStockRequest request,
    [FromServices] IInventoryItemRepository repository,
    [FromServices] IUnitOfWork uow,
    CancellationToken ct) =>
{
    var item = await repository.GetBySkuAsync(sku, ct);
    if (item == null) return Results.NotFound();

    if (request.Adjustment > 0)
        item.AddStock(request.Adjustment);
    else
        item.RemoveStock(Math.Abs(request.Adjustment));

    repository.Update(item);
    await uow.SaveChangesAsync(ct);
    return Results.Ok();
})
.RequireAuthorization();
```

### 4. Add Inventory Migration
Run `dotnet ef migrations add AddSellerAndThreshold --project src/Microservices/Inventory/Inventory.Infrastructure`

## Frontend Changes

### 5. Create Inventory Service
**New file:** `src/web/src/app/features/seller-dashboard/inventory.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class InventoryService {
  getSellerInventory(sellerId: string): Promise<InventoryItem[]> { ... }
  addStock(sku: string, quantity: number): Promise<void> { ... }
  adjustStock(sku: string, adjustment: number): Promise<void> { ... }
}
```

### 6. Create Inventory Store
**New file:** `src/web/src/app/features/seller-dashboard/inventory.store.ts`

```typescript
interface InventoryState {
  items: InventoryItem[];
  loading: boolean;
  error: string | null;
}
```

Methods: `loadInventory()`, `addStock()`, `adjustStock()`

### 7. Create Inventory List Component
**New file:** `src/web/src/app/features/seller-dashboard/inventory-list/inventory-list.ts`

Table view:
| SKU | Product Name | Stock | Status | Actions |
|-----|-------------|-------|--------|---------|
| ABC123 | Widget Pro | 15 | ✅ In Stock | [Add Stock] |
| DEF453 | Gadget Mini | 3 | ⚠️ Low Stock | [Add Stock] |
| GHI789 | Thing Max | 0 | ❌ Out of Stock | [Add Stock] |

- Color-coded status badges (green/orange/red)
- "Add Stock" button opens inline form
- Sort by: SKU, Name, Stock level, Status
- Filter: All, Low Stock, Out of Stock

### 8. Create Add Stock Dialog
**New file:** `src/web/src/app/features/seller-dashboard/add-stock-dialog/add-stock-dialog.ts`

Simple modal:
- SKU (read-only)
- Current stock display
- Quantity input (positive number)
- "Add" button
- Calls InventoryService.addStock()

### 9. Create Low Stock Alert Component
**New file:** `src/web/src/app/features/seller-dashboard/low-stock-alert/low-stock-alert.ts`

- Banner at top of inventory page
- "X products are low on stock" with link to filtered view
- Uses InventoryStore.lowStockItems computed signal

### 10. Add Inventory Tab to Seller Dashboard
**File:** `src/web/src/app/features/seller-dashboard/dashboard-page/dashboard-page.ts`

Add "Inventory" tab alongside Products, Orders, Settings.

### 11. Create Inventory Routes
**File:** `src/web/src/app/features/seller-dashboard/seller.routes.ts`

```typescript
{ path: 'inventory', loadComponent: () => import('./inventory-list/inventory-list').then(m => m.InventoryListComponent) },
```

### 12. Update Seller Models
**File:** `src/web/src/app/features/seller-dashboard/seller.models.ts`

Add:
```typescript
export interface InventoryItem {
  id: string;
  sku: string;
  quantity: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isOutOfStock: boolean;
  lastUpdated: string;
}
```

## Files to Modify/Create

| Action | File |
|--------|------|
| MODIFY | `Inventory.Domain/Aggregates/InventoryItem.cs` |
| MODIFY | `Inventory.Infrastructure/Persistence/IInventoryItemRepository.cs` |
| MODIFY | `Inventory.Infrastructure/Persistence/InventoryItemRepository.cs` |
| MODIFY | `Inventory.API/Endpoints/InventoryEndpoints.cs` |
| CREATE | EF Migration |
| CREATE | `src/web/src/app/features/seller-dashboard/inventory.service.ts` |
| CREATE | `src/web/src/app/features/seller-dashboard/inventory.store.ts` |
| CREATE | `src/web/src/app/features/seller-dashboard/inventory-list/inventory-list.ts` |
| CREATE | `src/web/src/app/features/seller-dashboard/add-stock-dialog/add-stock-dialog.ts` |
| CREATE | `src/web/src/app/features/seller-dashboard/low-stock-alert/low-stock-alert.ts` |
| MODIFY | `src/web/src/app/features/seller-dashboard/dashboard-page/dashboard-page.ts` |
| MODIFY | `src/web/src/app/features/seller-dashboard/seller.routes.ts` |
| MODIFY | `src/web/src/app/features/seller-dashboard/seller.models.ts` |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. `dotnet test tests/UnitTests/Inventory.UnitTests/` — passes
4. Manual: Seller dashboard → Inventory tab visible
5. Manual: Inventory list shows products with stock levels
6. Manual: Low stock items highlighted
7. Manual: Add stock → quantity updates
8. Manual: Filter by low stock/out of stock
9. Manual: Only seller's own products shown
