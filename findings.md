# Findings — Seeder + Rozetka Scraper Review (2026-06-08)

## Current Database State

| Table | Count | Expected |
|-------|-------|----------|
| Products | 5 | ✅ |
| Skus | **0** | 5 (one per product) |
| SkuAttributeValues | **0** | ~80 (16 attrs × 5) |
| ProductAttributeValues | **0** | 0 (correct — no product-level attrs set) |
| ProductVariantAxes | **83** | Should be ~10 (only color/storage/RAM) |
| AttributeDefinitions | **20** | OK but all forced to Select type |
| Categories | 1 | ✅ |

**All products are Status=0 (Draft)** — activation fails because it requires at least 1 active SKU.

## Root Cause: Why 0 SKUs?

### The Data Flow
```
Rozetka Scraper → catalog.json → Seeder.App → Gateway (YARP) → Catalog API
```

### Tracing the failure
1. `ProductSeeder.CreateSkuAsync()` calls `POST /api/catalog/products/{id}/skus`
2. The endpoint has `.RequireAuthorization()` — requires authenticated user
3. The `AddSkuHandler` has TWO validation stages:
   - **FluentValidation** (AddSkuValidator): checks SkuCode regex, Price > 0, Currency length → **PASSES** (ROZ-586833859 matches regex, price=35999>0, currency=UAH)
   - **Handler validation**: validates Select-type attribute values against AllowedValues

4. **The handler has debug logging that writes to `d:\code\Microservices\sku_errors.txt`** — this file does NOT exist, which means either:
   - The handler was never reached (auth/routing failure), OR
   - The `File.AppendAllText` failed silently in the Aspire process context, OR
   - The validation passed but `product.AddSku()` threw

5. Most likely: **Select-type attribute value mismatch** — the handler validates ALL 20 Select-type attributes against AllowedValues. Even one mismatch → 400 Bad Request. The `File.AppendAllText` path may not be writable from the Catalog API process.

### Specific Validation Issues

The scraped variant attributes include ALL spec fields (14-19 per product). The seeder sends these as `TypedAttributes`. The handler validates each against `AllowedValues`:

```csharp
// AddSkuHandler lines 54-73
foreach (var def in selectDefs)
{
    if (request.TypedAttributes?.TryGetValue(def.Key, out var value) == true
        && !string.IsNullOrWhiteSpace(value))
    {
        if (!def.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            // FAILS → 400 Bad Request
            File.AppendAllText(@"d:\code\Microservices\sku_errors.txt", ...);
            return Result<SkuDto>.Failure(msg, "VALIDATION_ERROR");
        }
    }
}
```

**Possible mismatches:**
- `brand`: scraped value "Intel" (from variant attrs) → seeder overwrites to product.Brand "Acer" → "Acer" IS in AllowedValues ✓
- `modelnyy-rik`: scraped value "2025" → AllowedValues ["2021", "2025", "2026", "2023"] ✓
- `diahonal-ekranu`: `15.6 "` → AllowedValues have escaped quotes in JSONB → encoding mismatch possible ⚠️

## Design Issues (Scraper → Seeder → API)

### Issue 1: ALL Attributes Become Variant Axes
`ProductSeeder` lines 56-68: takes ALL attribute keys from the first variant and sets them as `VariantAxisIds`. This creates 14-19 axes per product. Only `color`, `storage`, `ram` should be axes.

### Issue 2: ALL Attributes Forced to Select Type
`AttributeStep` line 58: `Target: 1` (Sku), `ValueType: 2` (Select) for ALL attributes. Attributes like `protsesor` (CPU model) are free-text, not Select.

### Issue 3: Each Product Has Only 1 Variant
The scraper scrapes individual product pages. Each "product" in catalog.json has exactly 1 variant. There are no multi-variant products (e.g., iPhone in 3 colors × 2 storage sizes = 6 SKUs).

### Issue 4: Variant Axes Are Not Distinguished from Spec Attributes
The scraper treats ALL Rozetka specs as attributes. The seeder treats ALL attributes as variant axes. There's no distinction between:
- **Variant axes** (color, storage, RAM) — what the user CHOOSES
- **Product specs** (CPU, screen, weight) — what DESCRIBES the product

### Issue 5: Debug Logging Uses Hardcoded Windows Path
`AddSkuHandler` lines 49, 69: `File.AppendAllText(@"d:\code\Microservices\sku_errors.txt", ...)` — this path may not be writable from the Catalog API's Aspire process.

## Entity/DTO/Endpoint Summary

### Domain Entities (Catalog.Domain)
- **Product** (AggregateRoot): Name, Description, Brand, CategoryId, StoreId, Status, ImageUrl, Tags
  - **Sku** (child entity): SkuCode, Price, TypedAttributes (jsonb), FlexibleAttributes (jsonb), Status
  - **ProductAttributeValue** (child): AttributeDefinitionId, Value
  - **ProductVariantAxis** (child): AttributeDefinitionId, SortOrder
- **Category**: Name, Slug, ParentCategoryId
  - **AttributeDefinition** (child): Key, DisplayName, Target (Product/Sku), ValueType (Text/Number/Select), AllowedValues, IsFilterable, IsRequired
- **SkuAttributeValue** (child of Sku): AttributeDefinitionId, Value

### Key DTOs (Catalog.Application.DTOs)
- **ProductDto**: Id, Name, Description, CategoryId, CategoryName, Status, ImageUrl, Brand, StoreId, Tags, Skus[], CreatedAt, UpdatedAt
- **SkuDto**: Id, SkuCode, Price, Currency, Status, ImageUrl, TypedAttributes, FlexibleAttributes, CreatedAt
- **ProductListDto**: (for paginated listing)
- **VariantMatrixDto**: (for variant picker)

### Key Endpoints (Catalog.API)
| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| POST | /api/catalog/products | ✅ | Create product |
| GET | /api/catalog/products/{id} | — | Get product with SKUs |
| GET | /api/catalog/products/ | — | List (paginated, filterable) |
| POST | /api/catalog/products/{id}/skus | ✅ | Add SKU to product |
| DELETE | /api/catalog/products/{id}/skus/{skuId} | ✅ | Remove SKU |
| PUT | /api/catalog/products/{id}/activate | ✅ | Activate product |
| GET | /api/catalog/products/{id}/variant-matrix | — | Variant picker data |

### Scraper Output (catalog.json)
```json
{
  "categories": [{ "id": "ноутбуки", "parentId": null, "name": "Ноутбуки", "url": "..." }],
  "attributeDefinitions": [{ "categoryId": "ноутбуки", "name": "color", "possibleValues": [...] }],
  "baseProducts": [{ "externalId": "hash", "categoryId": "ноутбуки", "title": "...", "brand": "Acer" }],
  "productVariants": [{ "productExternalId": "hash", "sku": "ROZ-586833859", "price": 35999, "attributes": {...} }]
}
```
