# Plan 04: Product Detail Page Enhancements

## Goal
Enhance the Product Detail Page with Sticky Buy Box, "Frequently Bought Together" bundles, stock availability indicators, and the "Add to Cart" button integration.

## Context
- **Current state:** ProductDetailComponent shows image, name, price, description, tags, SKU. Has "Add to Cart" button but no stock check. No sticky buy box. No cross-sells.
- **Target state:** Amazon/AliExpress-style PDP with pinned add-to-cart, stock indicators, bundle suggestions.
- **Design ref:** `plans/future_design/product_details.md`
- **Backend gaps:** No stock availability check endpoint for single SKU (Inventory has GET /api/inventory/items/{sku} — exists)

## Prerequisites
- Catalog.API has GET /api/catalog/products/{id} — exists
- Inventory.API has GET /api/inventory/items/{sku} — exists
- CartStore has addToCart — exists

## Backend Changes

### 1. Add Stock Check to Product Response
**File:** `src/Microservices/Catalog/Catalog.Application/DTOs/ProductDto.cs`

Add optional stock fields (populated by querying Inventory.API or via event sync):
```csharp
public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId,
    string CategoryName,
    string Sku,
    string? ImageUrl,
    List<string> Tags,
    string Status,
    int? StockQuantity,  // NEW
    bool? InStock);       // NEW
```

**Option A (simpler):** Frontend makes separate call to Inventory.API for stock check.
**Option B (better):** Catalog.API enriches product with stock from Inventory via gRPC/HTTP.

Recommend **Option A** for now (no cross-service dependency).

### 2. Add "Frequently Bought Together" Query
**File:** `src/Microservices/Catalog/Catalog.API/Endpoints/ProductEndpoints.cs`

```csharp
group.MapGet("/{id:guid}/recommendations", async (
    Guid id,
    ISender sender,
    CancellationToken ct) =>
{
    var result = await sender.Send(new GetProductRecommendationsQuery(id), ct);
    return Results.Ok(result);
})
.WithName("GetProductRecommendations");
```

**New files:**
- `Catalog.Application/Queries/GetProductRecommendationsQuery.cs`
- `Catalog.Application/Queries/GetProductRecommendationsHandler.cs`

Simple implementation: Return products from the same category, excluding the current product. Limit to 3-4 items.

## Frontend Changes

### 3. Create Inventory Service
**New file:** `src/web/src/app/core/services/inventory.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class InventoryService {
  private http = inject(HttpClient);

  checkStock(sku: string): Promise<InventoryItem> {
    return firstValueFrom(this.http.get<InventoryItem>(`/api/inventory/items/${sku}`));
  }
}
```

### 4. Update Product Detail Component
**File:** `src/web/src/app/features/catalog/product-detail/product-detail.ts`

Add:
- **Stock indicator:** Show "In Stock" (green), "Out of Stock" (red), "Only X left" (orange, when < 5)
- **Quantity selector:** +/- buttons with max = stock quantity
- **"Add to Cart" integration:** Call CartStore.addToCart() with product price
- **Sticky Buy Box:** Use CSS `position: sticky` on the buy box section (price + add-to-cart + quantity)
- **Mobile:** Sticky buy box at bottom of screen

### 5. Create Buy Box Component
**New file:** `src/web/src/app/features/catalog/components/buy-box/buy-box.ts`

Inputs: product (Product), stock (InventoryItem | null)
- Price display
- Stock status badge
- Quantity selector
- "Add to Cart" button (disabled if out of stock)
- "Buy Now" button (direct to checkout)

### 6. Create Frequently Bought Together Component
**New file:** `src/web/src/app/features/catalog/components/frequently-bought-together/frequently-bought-together.ts`

- Shows 2-4 related products from same category
- Each with checkbox, image, name, price
- "Add all X to Cart" button with total price
- Loads via `GET /api/catalog/products/{id}/recommendations`

### 7. Create Stock Indicator Component
**New file:** `src/web/src/app/shared/components/stock-indicator/stock-indicator.ts`

Inputs: quantity (number | null), loading (boolean)
- `null` → "Checking availability..."
- `0` → "Out of Stock" (red)
- `1-4` → "Only X left in stock" (orange)
- `5+` → "In Stock" (green)

### 8. Update Product Card for Stock Badge
**File:** `src/web/src/app/features/catalog/components/product-card/product-card.ts`

Add small stock badge overlay on product cards (optional, for search results).

## Files to Modify/Create

| Action | File |
|--------|------|
| MODIFY | `Catalog.Application/DTOs/ProductDto.cs` |
| CREATE | `Catalog.Application/Queries/GetProductRecommendationsQuery.cs` |
| CREATE | `Catalog.Application/Queries/GetProductRecommendationsHandler.cs` |
| MODIFY | `Catalog.API/Endpoints/ProductEndpoints.cs` |
| CREATE | `src/web/src/app/core/services/inventory.service.ts` |
| MODIFY | `src/web/src/app/features/catalog/product-detail/product-detail.ts` |
| CREATE | `src/web/src/app/features/catalog/components/buy-box/buy-box.ts` |
| CREATE | `src/web/src/app/features/catalog/components/frequently-bought-together/frequently-bought-together.ts` |
| CREATE | `src/web/src/app/shared/components/stock-indicator/stock-indicator.ts` |
| MODIFY | `src/web/src/app/features/catalog/components/product-card/product-card.ts` |
| MODIFY | `src/web/src/app/features/catalog/catalog.models.ts` |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. `dotnet test tests/UnitTests/Catalog.UnitTests/` — passes
4. Manual: Product detail → stock indicator shows correct status
5. Manual: Scroll down → buy box stays pinned
6. Manual: "Add to Cart" → item added, drawer opens
7. Manual: "Frequently Bought Together" → shows related products
8. Manual: "Add all to Cart" → bundle added
9. Manual: Out of stock product → button disabled
