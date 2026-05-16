# Plan 01: Global Header & Catalog Mega-Menu

## Goal
Transform the current basic header into a Rozetka/Prom-style global header with a prominent Catalog button that opens a mega-menu overlay. Replace the persistent left sidebar category navigation.

## Context
- **Current state:** Header has logo, basic nav links, user menu. Categories shown in a persistent left sidebar on the catalog page.
- **Target state:** Header with Catalog button + huge search bar + utility icons. Catalog button opens a mega-menu with root categories (left column) and subcategories (right content area).
- **Design ref:** `plans/future_design/catalog_navigation.md`, `plans/future_design/homepage_layout.md`

## Prerequisites
- Catalog.API has `GET /api/catalog/categories` returning flat list
- Need a new endpoint or frontend transformation to build a category tree (parent-child hierarchy)

## Backend Changes

### 1. Add Category Tree Endpoint
**File:** `src/Microservices/Catalog/Catalog.API/Endpoints/CategoryEndpoints.cs`

Add `GET /api/catalog/categories/tree` that returns nested category structure:
```csharp
group.MapGet("/tree", async (ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetCategoryTreeQuery(), ct);
    return Results.Ok(result);
});
```

**New files needed:**
- `Catalog.Application/Queries/GetCategoryTreeQuery.cs`
- `Catalog.Application/Queries/GetCategoryTreeHandler.cs`
- `Catalog.Application/DTOs/CategoryTreeDto.cs` (with `List<CategoryTreeDto> Children`)

The handler should:
1. Load all categories from DB
2. Build tree in memory (root categories → children → grandchildren)
3. Return up to 3 levels deep

## Frontend Changes

### 2. Update Header Component
**File:** `src/web/src/app/shared/components/header/header.ts`

Current header has: logo, nav links, user dropdown. Transform to:
- **Left:** Logo + "Catalog" button (dark background, hamburger icon)
- **Center:** Wide search bar with placeholder "I am looking for..." + search button
- **Right:** Language toggle, Wishlist icon, Profile dropdown, Cart icon with badge

The search bar should:
- Use `CatalogStore.searchQuery` signal
- On submit, navigate to `/catalog?q={query}`
- Show autocomplete suggestions (debounced 350ms) from `Search.API`

### 3. Create Mega-Menu Component
**New file:** `src/web/src/app/shared/components/mega-menu/mega-menu.ts`

- Full-width overlay that drops down when Catalog button is clicked
- **Left column:** Root categories with icons (vertical list)
- **Right content area:** Subcategories in columns when a root category is hovered
- **Optional:** Promotional banner space on far right
- Close on click outside or Escape key
- Use `@defer` for lazy loading

### 4. Create Category Tree Service
**New file:** `src/web/src/app/core/services/category-tree.service.ts`

- Fetches category tree on app init (via `APP_INITIALIZER`)
- Caches aggressively (localStorage or in-memory signal)
- Exposes `categoryTree` signal for mega-menu consumption

### 5. Remove Category Sidebar
**File:** `src/web/src/app/features/catalog/components/category-sidebar/category-sidebar.ts`

- Remove or repurpose (keep as mobile fallback if needed)
- Update `ProductListComponent` to remove sidebar import

### 6. Update Catalog Routes
**File:** `src/web/src/app/features/catalog/catalog.routes.ts`

- Remove category sidebar from layout
- Keep product list and product detail routes

## Files to Modify/Create

| Action | File |
|--------|------|
| CREATE | `Catalog.Application/Queries/GetCategoryTreeQuery.cs` |
| CREATE | `Catalog.Application/Queries/GetCategoryTreeHandler.cs` |
| CREATE | `Catalog.Application/DTOs/CategoryTreeDto.cs` |
| MODIFY | `Catalog.API/Endpoints/CategoryEndpoints.cs` |
| CREATE | `src/web/src/app/core/services/category-tree.service.ts` |
| CREATE | `src/web/src/app/shared/components/mega-menu/mega-menu.ts` |
| MODIFY | `src/web/src/app/shared/components/header/header.ts` |
| MODIFY | `src/web/src/app/app.config.ts` (add APP_INITIALIZER for category tree) |
| MODIFY | `src/web/src/app/features/catalog/product-list/product-list.ts` (remove sidebar) |
| MODIFY | `src/web/src/app/features/catalog/catalog.routes.ts` |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. `dotnet test tests/UnitTests/Catalog.UnitTests/` — passes
4. Manual: Click Catalog button → mega-menu opens with categories
5. Manual: Hover root category → subcategories appear
6. Manual: Click subcategory → navigates to filtered catalog
7. Manual: Search bar → autocomplete → navigate to results
8. Manual: Mobile → mega-menu works (responsive)
