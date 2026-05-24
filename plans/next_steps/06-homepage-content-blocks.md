# Plan 06: Homepage Content Blocks

## Goal
Transform the current catalog-only homepage into a promotional hub with hero banner, category tiles, recent views, and personalized sections.

## Context
- **Current state:** Homepage redirects to /catalog. No dedicated homepage. Product grid is the main view.
- **Target state:** Promotional homepage with hero carousel, category tiles, recent views, deal sections. Catalog becomes a separate route.
- **Design ref:** `plans/future_design/homepage_layout.md`

## Prerequisites
- Catalog.API has GET /api/catalog/categories — exists
- Catalog.API has GET /api/catalog/products with pagination — exists
- Search.API has GET /api/search/products — exists

## Backend Changes

### 1. Add Featured Products Endpoint
**File:** `src/Microservices/Catalog/Catalog.API/Endpoints/ProductEndpoints.cs`

```csharp
group.MapGet("/featured", async (ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetFeaturedProductsQuery(), ct);
    return Results.Ok(result);
})
.WithName("GetFeaturedProducts");
```

**New files:**
- `Catalog.Application/Queries/GetFeaturedProductsQuery.cs` + Handler
- Returns top N products by some criteria (newest, highest rated, or tagged as "featured")

### 2. Add Product Tags for Featured/Deals
**File:** `src/Microservices/Catalog/Catalog.Domain/Aggregates/Product.cs`

Products already have Tags (List<string>). Use tags like "featured", "deal-of-the-day", "new-arrival" to categorize homepage content.

### 3. Add Homepage Banners Endpoint (Optional)
**New file:** `src/Microservices/Catalog/Catalog.API/Endpoints/BannerEndpoints.cs`

Simple CRUD for promotional banners:
- `GET /api/catalog/banners` — active banners
- `POST /api/catalog/banners` — create (admin)
- `PUT /api/catalog/banners/{id}` — update (admin)
- `DELETE /api/catalog/banners/{id}` — delete (admin)

**New files:**
- `Catalog.Domain/Aggregates/Banner.cs` (ImageUrl, Title, LinkUrl, Position, IsActive, StartDate, EndDate)
- `Catalog.Application/Commands/CreateBanner/`
- `Catalog.Application/Queries/ListActiveBanners/`

## Frontend Changes

### 4. Create Homepage Component
**New file:** `src/web/src/app/features/home/home-page/home-page.ts`

Layout:
```
┌─────────────────────────────────────────────┐
│ Hero Banner Carousel (full width)           │
├─────────────────────────────────────────────┤
│ Category Tiles (grid of 6-8 popular cats)   │
├─────────────────────────────────────────────┤
│ "Deal of the Day" (countdown + products)    │
├─────────────────────────────────────────────┤
│ "New Arrivals" (product carousel)           │
├─────────────────────────────────────────────┤
│ "Recommended for You" (if logged in)        │
├─────────────────────────────────────────────┤
│ "Recently Viewed" (localStorage-based)      │
└─────────────────────────────────────────────┘
```

### 5. Create Hero Banner Component
**New file:** `src/web/src/app/features/home/components/hero-banner/hero-banner.ts`

- Image carousel with auto-advance (5s interval)
- Navigation dots
- Previous/Next arrows
- Responsive (full width)
- Uses banner data from API or hardcoded for MVP

### 6. Create Category Tiles Component
**New file:** `src/web/src/app/features/home/components/category-tiles/category-tiles.ts`

- Grid of 6-8 top-level categories
- Each tile: category icon/image + name
- Click navigates to `/catalog?category={id}`
- Uses CategoryTreeService (from Plan 01)

### 7. Create Product Carousel Component
**New file:** `src/web/src/app/features/home/components/product-carousel/product-carousel.ts`

Reusable horizontal scrollable product list:
- Section title (e.g., "New Arrivals")
- Left/Right scroll arrows
- Product cards (reuse ProductCardComponent)
- Responsive (shows 2-4 items depending on screen)

### 8. Create Deal of the Day Component
**New file:** `src/web/src/app/features/home/components/deal-of-the-day/deal-of-the-day.ts`

- Countdown timer to deal end
- Product with original/sale price
- Progress bar (X% claimed)
- "View Deal" button

### 9. Create Recently Viewed Service
**New file:** `src/web/src/app/core/services/recently-viewed.service.ts`

- Stores viewed product IDs in localStorage (max 20)
- Exposes `recentlyViewed` signal
- Called from ProductDetailComponent on init

### 10. Create Home Store
**New file:** `src/web/src/app/features/home/home.store.ts`

```typescript
interface HomeState {
  banners: Banner[];
  featuredProducts: ProductListItem[];
  newArrivals: ProductListItem[];
  deals: ProductListItem[];
  loading: boolean;
}
```

### 11. Create Home Routes
**New file:** `src/web/src/app/features/home/home.routes.ts`

```typescript
export const HOME_ROUTES: Routes = [
  { path: '', component: HomePageComponent },
];
```

### 12. Update App Routes
**File:** `src/web/src/app/app.routes.ts`

Change default redirect from `/catalog` to `/home`:
```typescript
{ path: '', redirectTo: 'home', pathMatch: 'full' },
{ path: 'home', loadChildren: () => import('./features/home/home.routes') },
```

Keep `/catalog` as a separate route for browsing.

### 13. Update Header for Homepage
**File:** `src/web/src/app/shared/components/header/header.ts`

On homepage, search bar should be more prominent (larger, centered). On other pages, standard size.

## Files to Modify/Create

| Action | File |
|--------|------|
| CREATE | `Catalog.Application/Queries/GetFeaturedProductsQuery.cs` + Handler |
| MODIFY | `Catalog.API/Endpoints/ProductEndpoints.cs` |
| CREATE | `src/web/src/app/features/home/home-page/home-page.ts` |
| CREATE | `src/web/src/app/features/home/home.store.ts` |
| CREATE | `src/web/src/app/features/home/home.routes.ts` |
| CREATE | `src/web/src/app/features/home/components/hero-banner/hero-banner.ts` |
| CREATE | `src/web/src/app/features/home/components/category-tiles/category-tiles.ts` |
| CREATE | `src/web/src/app/features/home/components/product-carousel/product-carousel.ts` |
| CREATE | `src/web/src/app/features/home/components/deal-of-the-day/deal-of-the-day.ts` |
| CREATE | `src/web/src/app/core/services/recently-viewed.service.ts` |
| MODIFY | `src/web/src/app/app.routes.ts` |
| MODIFY | `src/web/src/app/features/catalog/product-detail/product-detail.ts` (track views) |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. Manual: Homepage loads with hero banner
4. Manual: Category tiles navigate to catalog
5. Manual: Product carousels scroll horizontally
6. Manual: Recently viewed updates as user browses
7. Manual: Mobile responsive layout
8. Manual: /catalog still works as separate route
