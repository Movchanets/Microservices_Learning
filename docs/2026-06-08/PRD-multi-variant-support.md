# PRD: Multi-Variant Product Support — Scraper, Gallery, and Specs

## Problem Statement

The marketplace catalog currently supports single-variant products scraped from Rozetka. Each Rozetka product page yields one SKU, even when the product has multiple configurations (e.g., a MacBook with 4 colors × 2 storage options = 8 variants). This means:

1. **Incomplete catalog data** — products with multiple variants (color, storage, RAM) are represented as single SKUs, missing the variant matrix that buyers expect.
2. **No product images** — the seeder pipeline (MediaStep) already supports uploading per-SKU images via the Media API, but the scraper only captures one variant's images per page.
3. **No specs table on product detail** — non-axis attributes (CPU, screen type, weight) are stored in SKU TypedAttributes but not displayed to buyers.
4. **Inconsistent scraped values** — attribute values have formatting inconsistencies (`15.6 "` vs `15.6"`, `DDR5` vs `DDR 5`) that break filtering and facet counts.
5. **Manual variant axis classification** — the `isVariantAxis` flag in `catalog.json` is currently set manually; the scraper should auto-detect which attributes vary across selectors.

## Solution

Upgrade the Rozetka scraper to detect and click through variant selectors (color, storage, RAM), collecting per-variant data and images. Add normalization and auto-classification of variant axes. Wire the frontend to display per-SKU galleries and a specs table on the product detail page. Write a browser-level E2E test covering the full variant selection → cart → order flow.

## User Stories

1. As a **buyer**, I want to see all available color/storage/RAM options for a laptop on the product detail page, so that I can choose the configuration I want.
2. As a **buyer**, I want the product images to change when I select a different variant (e.g., different color), so that I see the actual product I'm buying.
3. As a **buyer**, I want to see a specs table showing non-axis attributes (CPU, screen type, weight, battery) for the selected variant, so that I can compare configurations.
4. As a **buyer**, I want the specs table to update when I switch variants, so that I see variant-specific specs (e.g., weight differs between 16GB and 64GB configs).
5. As a **buyer**, I want to add a specific variant to my cart, so that my order contains the exact configuration I selected.
6. As a **buyer**, I want to see the correct price for the selected variant in the buy box, so that I'm not surprised at checkout.
7. As a **buyer**, I want variant axes (color, storage, RAM) to appear as filterable facets in search results, so that I can narrow down products by configuration.
8. As a **seller**, I want scraped products to have all their variants pre-populated, so that I don't have to manually create SKUs for each configuration.
9. As a **seller**, I want per-SKU images to be automatically uploaded during seeding, so that each variant has its own gallery.
10. As a **developer**, I want the scraper to auto-detect which attributes are variant axes (vary across selectors) vs specs (same across all variants), so that I don't need to manually classify them in `catalog.json`.
11. As a **developer**, I want scraped attribute values to be normalized (trimmed, deduplicated spaces, standardized units), so that filtering and faceting work correctly.
12. As a **developer**, I want the scraper to download images locally during scraping, so that the seeder doesn't depend on Rozetka CDN URLs being available at seed time.
13. As a **QA engineer**, I want a browser-level E2E test that covers variant selection → add to cart → checkout → order verification, so that the full buyer flow is tested.
14. As a **buyer**, I want unavailable variant combinations to be visually disabled in the variant picker, so that I don't attempt to select out-of-stock configurations.
15. As a **buyer**, I want the first available variant to be auto-selected when I land on the product detail page, so that I see a valid price and image immediately.
16. As a **developer**, I want the `ProductSearchDocument.VariantAxes` field to be populated when SKUs are created, so that search facets work correctly (already implemented via `SkuCreatedConsumer`).
17. As a **developer**, I want the MediaStep in the seeder pipeline to upload images with `TargetType=SKU, TargetId=skuId`, so that per-SKU galleries are created (already implemented).
18. As a **developer**, I want the frontend to fetch product-level gallery from `/api/media/gallery/PRODUCT/{productId}` and per-SKU gallery from `/api/media/gallery/SKU/{skuId}`, so that galleries are correctly scoped.
19. As a **buyer**, I want to see a breadcrumb showing the selected variant's attributes (e.g., "Gold · 512GB") below the product name, so that I know which variant I'm viewing.
20. As a **developer**, I want the scraper to group multiple variants under one `baseProduct` in `catalog.json`, so that the seeder creates one Product with multiple SKUs.
21. As a **developer**, I want the `rozetka-transformer.ts` to build proper variant groups from the scraper's multi-variant output, so that `catalog.json` has the correct structure.
22. As a **developer**, I want the seeder to clear and re-seed the database with fresh scraped data, so that the catalog reflects the latest Rozetka listings.
23. As a **buyer**, I want the variant picker to show color swatches (if available) instead of text buttons for color axes, so that the UI is more intuitive.
24. As a **developer**, I want the scraper to handle Rozetka pages where selectors are not standard dropdowns (e.g., clickable tiles, image swatches), so that all variant types are captured.
25. As a **developer**, I want the scraper to retry failed image downloads, so that transient network errors don't result in missing images.

## Implementation Decisions

### 1. Per-SKU Gallery Model

Gallery entries use `TargetType=SKU, TargetId=skuId` for per-SKU images. The Media API upload handler automatically creates `GalleryEntry` with `SkuId` set when `TargetType=SKU`. No separate gallery creation step needed.

- **Endpoint for product gallery**: `GET /api/media/gallery/PRODUCT/{productId}`
- **Endpoint for per-SKU gallery**: `GET /api/media/gallery/SKU/{skuId}`

### 2. Scraper Downloads Images Locally

The scraper downloads variant images during scraping and saves them to a local `images/` directory. The `catalog.json` `images` field contains local file paths, not URLs. This decouples scraping from Media API availability and Rozetka CDN stability.

### 3. Normalization in the Scraper

Attribute value normalization (trim, deduplicate spaces, standardize units) happens in the scraper before writing to `catalog.json`. The `catalog.json` contains clean, normalized data. This keeps `catalog.json` as the source of truth for normalized values.

### 4. Auto-Classification of Variant Axes

The scraper auto-detects which attributes are variant axes by analyzing which attributes vary across selector options. Attributes that change when clicking through color/storage/RAM selectors are marked as variant axes (`isVariantAxis: true`). Attributes that remain constant across all variants are marked as specs (`isVariantAxis: false`).

### 5. Specs Table from Selected SKU

The product detail page renders a specs table from `selectedSku().TypedAttributes`, excluding keys present in `store.variantMatrix().axes`. The table updates when the user switches variants. This is a client-side computed — no new API endpoint needed.

### 6. Frontend Gallery Fetching

The product detail store fetches two galleries:
- Product-level: `GET /api/media/gallery/PRODUCT/{productId}` (shown by default)
- Per-SKU: `GET /api/media/gallery/SKU/{skuId}` (shown when variant is selected, merged/overrides product gallery)

### 7. Search Index Variant Axes (Already Implemented)

`SkuCreatedConsumer` populates `ProductSearchDocument.VariantAxes` via `AddVariantAxisValueAsync()` atomic Elasticsearch script. No changes needed.

### 8. MediaStep in Seeder (Already Implemented)

The `MediaStep` (Step 7) in the seeder pipeline iterates all variants per product and uploads images with `TargetType=SKU`. Supports both local files and HTTP URLs. No changes needed.

## Testing Decisions

### Test Seams

1. **Scraper output** — Test the scraper's `rozetka-transformer.ts` by verifying `catalog.json` output has correct variant groups, normalized values, and `isVariantAxis` flags. This is the highest-value seam — clean output here means the rest of the pipeline works.

2. **Media API gallery endpoints** — Test `GET /api/media/gallery/PRODUCT/{id}` and `GET /api/media/gallery/SKU/{id}` return correctly scoped galleries. Existing `laptop-variant-matrix.spec.ts` already covers per-SKU gallery isolation via API.

3. **Product detail page (browser E2E)** — Test variant selection → image change → add to cart → checkout → order verification. This is the new E2E test in Item 6.

4. **Specs table rendering** — Test that `specEntries` computed correctly filters out axis keys and renders the remaining attributes.

### Prior Art

- `laptop-variant-matrix.spec.ts` — Comprehensive API-level tests for variant matrix, gallery isolation, price changes, uniqueness validation
- `product-sku-crud.spec.ts` — Full CRUD lifecycle via API
- `checkout-flow.spec.ts` — Basic checkout page tests (needs expansion for variant flow)
- `seller-dashboard.spec.ts` — Browser-level seller dashboard tests

### Test Approach

- **API-level tests** for scraper output validation and Media API gallery endpoints (fast, reliable)
- **Browser-level E2E test** for the full buyer flow (variant selection → cart → order)
- **Unit tests** for normalization functions and auto-classification logic in the scraper

## Out of Scope

- **Seller-facing gallery management UI** — sellers can already upload per-SKU images via the product form; no changes needed
- **Search faceting UI** — the search index already has `VariantAxes`; the frontend search filter UI is a separate concern
- **Inventory integration per variant** — stock is already tracked per SKU; no changes needed
- **Price matrix UI** — showing a grid of prices across variant axes (e.g., color × storage price matrix) is not included
- **Multi-language attribute values** — Ukrainian/English attribute value normalization (e.g., "Сріблястий" vs "Silver") is out of scope
- **Scraper for non-Rozetka sources** — only Rozetka scraping is in scope

## Further Notes

### Dependency Chain

The work items have a clear dependency chain:

```
Item 2 (Multi-variant scraper)
    → Item 3 (Regenerate catalog.json)
        → Item 1 (Frontend gallery fetch — needs SKU images in DB)
        → Item 9 (Specs table — needs TypedAttributes in DB)

Item 8 (Normalization + auto-classification) — can be done in parallel with Item 2

Item 6 (E2E test) — depends on Items 1 and 9 being complete
```

### Already Implemented (No Work Needed)

- MediaStep in seeder pipeline (Step 7)
- Seller dashboard variant matrix + per-SKU images
- Gateway routing (working fine)
- Search index variant axes (`SkuCreatedConsumer`)
- Variant picker + buy box on product detail page

### Priority Order

1. **Scraper multi-variant + normalization + auto-classification** (Items 2 + 8) — biggest piece, unblocks everything
2. **Regenerate catalog.json** (Item 3) — depends on scraper upgrade
3. **Frontend gallery fetch** (Item 1) — depends on SKU images in DB
4. **Specs table** (Item 9) — depends on TypedAttributes in DB
5. **E2E browser test** (Item 6) — depends on all above
