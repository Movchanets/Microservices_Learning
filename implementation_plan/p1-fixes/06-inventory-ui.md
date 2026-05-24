# P1-06 — Inventory Management UI

**Goal**: Add inventory management page for sellers/admins to view and manage stock levels.

**Fixes**: MISSING.md #5.2

**Depends on**: P1-04 (inventory list endpoint), P0-01 (auth)

---

## Models

File: `src/web/src/app/features/inventory/inventory.models.ts`
```typescript
export interface InventoryItem {
  sku: string;
  quantity: number;
  reservedQuantity: number;
  availableQuantity: number;
}
```

## Service

File: `src/web/src/app/features/inventory/inventory.service.ts`
```typescript
@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/inventory';

  async getItems(): Promise<InventoryItem[]> { ... }
  async addItem(sku: string, quantity: number): Promise<void> { ... }
  async addStock(sku: string, quantity: number): Promise<void> { ... }
}
```

## Store

File: `src/web/src/app/features/inventory/inventory.store.ts`

NgRx SignalStore with items, loading, error state.

## Page Component

File: `src/web/src/app/features/inventory/inventory-page/inventory-page.ts`

Table showing SKU, quantity, reserved, available. Actions: add stock.

## Route

File: `src/web/src/app/features/inventory/inventory.routes.ts`

Lazy-loaded at `/inventory` (admin) or `/seller/inventory` (seller).

## Done When
- [ ] Inventory list page with stock levels
- [ ] Add stock action
- [ ] Route protected by Seller/Admin role
