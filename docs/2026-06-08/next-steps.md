# Next Steps — Post Seeder Fix

## Immediate (Ready to Do)

### 1. Product Gallery from Scraper Images
**Status**: MediaStep already exists (Step 7) — iterates variants, uploads with `TargetType=SKU`, supports local files and URLs. Gallery wiring is complete. Gap is that scraper only produces 1 variant per page (see Item 2).

**Tasks**:
- [x] MediaStep in seeder pipeline: uploads variant images with `TargetType=SKU, TargetId=skuId` ✅
- [ ] Frontend: fetch per-SKU gallery from `/api/media/gallery/SKU/{skuId}` when variant is selected
- [ ] Frontend: fetch product-level gallery from `/api/media/gallery/PRODUCT/{productId}`

### 2. Scraper: Multi-Variant Product Support
**Status**: Scraper produces 1 variant per Rozetka page. Apple has 8 variants across color/storage selectors.

**Tasks**:
- [ ] Detect color/storage selectors on Rozetka product page
- [ ] Click through each selector option to collect variant-specific data (price, SKU code)
- [ ] Download variant images locally during scraping (save to `images/` dir, store file paths in catalog.json)
- [ ] Group variants under one `baseProduct` with shared specs
- [ ] Update `rozetka-transformer.ts` to build proper variant groups

### 3. Regenerate catalog.json with New Scraper
**Status**: Current catalog.json was manually updated with `isVariantAxis` flags. Needs fresh scrape.

**Tasks**:
- [ ] Run scraper with new `isVariantAxis` classification
- [ ] Verify output has correct axis/spec split
- [ ] Clear DB and re-seed with fresh data

## Short-Term (This Week)

### 4. Seller Dashboard: Add SKU Flow
**Status**: Already implemented — product-form has variant axis selector, VariantMatrixBuilder, per-SKU image galleries, and BulkAddSku integration.

**Tasks**:
- [x] Wire product-form's variant axis selector to `POST /api/catalog/products/{id}/skus` ✅
- [x] Show variant matrix in seller dashboard (editable grid) ✅
- [x] Allow seller to upload SKU-specific images ✅

### 5. Gateway Routing Fix
**Status**: Not blocking — frontend reaches all services through Gateway. May have been a transient issue.

**Tasks**:
- [ ] Investigate YARP service discovery — check if `http://catalog-api` resolves correctly
- [ ] Verify Aspire service registration in gateway's `Program.cs`
- [ ] Test: `GET /api/catalog/products` through gateway should proxy to Catalog API

### 6. End-to-End Test: Variant Selection → Cart → Order
**Status**: UI is fully implemented — variant picker, buy box, cart, checkout all exist. Need to write the browser-level E2E test.
**Tasks**:
- [ ] Select variant on product detail → verify correct SKU shown
- [ ] Add to cart → verify SKU ID and price
- [ ] Checkout → verify order contains correct SKU

## Medium-Term (Next Sprint)

### 7. Search Index with Variant Data
**Status**: Already implemented — `ProductSearchDocument.VariantAxes` is populated by `SkuCreatedConsumer` via `AddVariantAxisValueAsync()` atomic script.

### 8. Attribute Value Normalization & Auto-Classification
**Status**: To be implemented in the scraper. Scraper should normalize values (trim, deduplicate spaces, standardize units) AND auto-detect which attributes are variant axes (vary across selectors) vs specs (same across all variants).

### 9. Product Specs Display
**Status**: Not implemented. Data exists in `selectedSku().TypedAttributes`. Need to add `specEntries` computed that filters out variant axis keys and renders as a key-value table. Updates on variant switch.

## Known Issues

| Issue | Severity | Status |
|-------|----------|--------|
| Gateway returns 404 for /api/* | Low | Resolved — frontend OK |
| Product gallery not wired | Medium | Planned — MediaStep in seeder |
| Seller can't add SKUs via UI | Low | Resolved — product-form already has variant matrix + SKU CRUD |
| Scraper only gets 1 variant per page | Medium | Planned — multi-variant scraper |
| `brand` in SKU TypedAttributes but not variant axis | Low | Acceptable |
| Search index missing variant axes | Low | Resolved — SkuCreatedConsumer populates VariantAxes |
| Specs table not on product detail | Medium | Planned — filter TypedAttributes by non-axis keys |
