# Catalog Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Project Path** | `src/Microservices/Catalog/` |

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `Product` | Aggregate Root | Name, Description, Brand, **ImageUrl** (cached), Tags, Status (Draft/Active/Inactive/Deleted), CategoryId, StoreId, CreatedAt, UpdatedAt |
| `Sku` | Child Entity (of Product) | ProductId, SkuCode, Price (Money VO), Status, **ImageUrl** (cached), TypedAttributes (jsonb), FlexibleAttributes (jsonb), CreatedAt, UpdatedAt |
| `Category` | Entity | Name, Description, Slug, ParentCategoryId (tree), SortOrder, IsActive, AttributeDefinitions |
| `AttributeDefinition` | Entity | CategoryId, Key, DisplayName, Target (Product/Sku), ValueType, IsFilterable, IsRequired, SortOrder, AllowedValues |
| `ProductVariantAxis` | Child Entity (of Product) | ProductId, AttributeDefinitionId, SortOrder — declares which attributes form variant axes |
| `ProductAttributeValue` | Child Entity (of Product) | ProductId, AttributeDefinitionId, Value — product-level attribute values |
| `SkuAttributeValue` | Child Entity (of Sku) | SkuId, AttributeDefinitionId, Value — SKU-level attribute values |

**ImageUrl caching:** `Product.ImageUrl` and `Sku.ImageUrl` are denormalized caches of the primary gallery image. They're synced by Media integration events (see below). This avoids N+1 calls to Media.API in list views.

**Variant Axes:** Products can declare variant axes (e.g., Color, Storage) via `ProductVariantAxis`. When adding a SKU, the system validates that no existing active SKU has the same combination of variant-axis attributes, preventing duplicate variants.

## API Endpoints

### Products (`/api/catalog/products`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/featured` | `GetFeaturedProductsQuery` | Public |
| `POST` | `/by-ids` | `GetProductsByIdsQuery` | Public (max 100) |
| `GET` | `/sku/{sku}` | `GetProductBySkuQuery` | Public |
| `GET` | `/` | `ListProductsQuery` | Public (paginated, filterable by categoryId, storeId, search, status) |
| `GET` | `/{id}` | `GetProductByIdQuery` | Public |
| `POST` | `/` | `CreateProductCommand` | Authenticated |
| `PUT` | `/{id}` | `UpdateProductCommand` | Authenticated |
| `PATCH` | `/{id}/price` | `ChangePriceCommand` | Authenticated |
| `GET` | `/{id}/variant-matrix` | `GetVariantMatrixQuery` | Public |
| `PUT` | `/{id}/activate` | `ActivateProductCommand` | Authenticated |
| `PUT` | `/{id}/deactivate` | `DeactivateProductCommand` | Authenticated |
| `DELETE` | `/{id}` | `DeleteProductCommand` | Authenticated |

### SKUs (sub-resource at `/api/catalog/products/{id}/skus`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/api/catalog/products/skus/{skuId}` | `GetSkuByIdQuery` | Public |
| `POST` | `/api/catalog/products/{id}/skus` | `AddSkuCommand` | Authenticated |
| `POST` | `/api/catalog/products/{id}/skus/bulk` | `BulkAddSkuCommand` | Authenticated |
| `DELETE` | `/api/catalog/products/{id}/skus/{skuId}` | `RemoveSkuCommand` | Authenticated |
| `PATCH` | `/api/catalog/products/{id}/skus/{skuId}/price` | `ChangePriceCommand` | Authenticated |

### Categories (`/api/catalog/categories`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/tree` | `GetCategoryTreeQuery` | Public |
| `GET` | `/` | `ListCategoriesQuery` | Public |
| `POST` | `/` | `CreateCategoryCommand` | Authenticated |
| `PUT` | `/{id}` | `UpdateCategoryCommand` | Authenticated |
| `DELETE` | `/{id}` | `DeleteCategoryCommand` | Authenticated |
| `POST` | `/{id}/attributes` | `AddAttributeDefinition` (direct) | Authenticated |
| `GET` | `/{id}/attributes` | `GetAttributeDefinitions` (direct, supports `?includeInherited=true`) | Public |
| `DELETE` | `/{id}/attributes/{attrId}` | `RemoveAttributeDefinition` (direct) | Authenticated |

## Integration Events

### Published (via Outbox)

| Event | Domain Trigger |
|:---|:---|
| `ProductCreatedEvent` | Product.Create() — includes ProductId, Name, Description, CategoryId, CategoryName, Tags, ImageUrl, StoreId, Brand, Attributes, CreatedAt |
| `ProductUpdatedEvent` | Product.Update() / Activate() / Deactivate() |
| `ProductDeletedEvent` | Product.SoftDelete() |
| `ProductPriceChangedEvent` | Product price change (OldPrice, NewPrice, Currency) |
| `SkuCreatedIntegrationEvent` | Product.AddSku() — includes ProductId, SkuId, SkuCode, ProductName, StoreId, Price, Currency, TypedAttributes, FlexibleAttributes |
| `SkuDeletedEvent` | Product.RemoveSku() |
| `SkuPriceChangedEvent` | Product.ChangeSkuPrice() |

### Consumed (from Media.API)

| Event | Consumer | Action |
|:---|:---|:---|
| `MediaUploadedIntegrationEvent` | `MediaUploadedConsumer` | Updates Product/SKU.ImageUrl when IsPrimary=true |
| `GalleryUpdatedIntegrationEvent` | `GalleryUpdatedConsumer` | Updates Product/SKU.ImageUrl from primary gallery item |
| `MediaDeletedIntegrationEvent` | `MediaDeletedConsumer` | Clears Product/SKU.ImageUrl if WasPrimary |

These consumers implement the **hybrid caching pattern**: list views use the cached ImageUrl (no Media.API call), detail pages fetch full gallery via BFF.

## Current Status

- ✅ SKU refactor complete with all 6 domain events
- ✅ Media consumers for ImageUrl caching (upload, gallery update, delete)
- ✅ GetSkuById endpoint for SKU detail pages
- ✅ Variant matrix endpoint for variant picker UI
- ✅ Bulk SKU creation for variant combinations
- ✅ Product variant axes with uniqueness validation
- ✅ Category attribute definitions with inheritance
- 🟠 No Review entity implemented (planned, not built)

---

*Last Updated: 2026-06-20*
