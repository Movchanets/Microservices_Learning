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
| `Product` | Aggregate Root | Name, Description, Brand, **ImageUrl** (cached), Tags, Status (Draft/Active/Inactive/Deleted), CategoryId, StoreId |
| `Sku` | Child Entity (of Product) | ProductId, SkuCode, Price (Money VO), Status, **ImageUrl** (cached), TypedAttributes (jsonb), FlexibleAttributes (jsonb) |
| `Category` | Entity | Name, ParentId (tree), AttributeDefinitions |
| `AttributeDefinition` | Entity | Key, DisplayName, Target (Product/Sku), ValueType, IsFilterable, IsRequired, AllowedValues |
| `Review` | Aggregate Root | ProductId, UserId, Rating, Title, Body, Photos, SellerResponse |
| `ReviewVote` | Entity | ReviewId, UserId, IsHelpful |

**ImageUrl caching:** `Product.ImageUrl` and `Sku.ImageUrl` are denormalized caches of the primary gallery image. They're synced by Media integration events (see below). This avoids N+1 calls to Media.API in list views.

## API Endpoints

### Products (`/api/catalog/products`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/featured` | `GetFeaturedProductsQuery` | Public |
| `POST` | `/by-ids` | `GetProductsByIdsQuery` | Public (max 100) |
| `GET` | `/sku/{sku}` | `GetProductBySkuQuery` | Public |
| `GET` | `/` | `ListProductsQuery` | Public (paginated) |
| `GET` | `/{id}` | `GetProductByIdQuery` | Public |
| `POST` | `/` | `CreateProductCommand` | Authenticated |
| `PUT` | `/{id}` | `UpdateProductCommand` | Authenticated |
| `PATCH` | `/{id}/price` | `ChangePriceCommand` | Authenticated |
| `GET` | `/{id}/recommendations` | `GetProductRecommendationsQuery` | Public |
| `PUT` | `/{id}/activate` | `ActivateProductCommand` | Authenticated |
| `PUT` | `/{id}/deactivate` | `DeactivateProductCommand` | Authenticated |
| `DELETE` | `/{id}` | `DeleteProductCommand` | Authenticated |

### SKUs (sub-resource of Products)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/skus/{skuId}` | `GetSkuByIdQuery` | Public |
| `POST` | `/{id}/skus` | `AddSkuCommand` | Authenticated |
| `DELETE` | `/{id}/skus/{skuId}` | `RemoveSkuCommand` | Authenticated |
| `PATCH` | `/{id}/skus/{skuId}/price` | `ChangePriceCommand` | Authenticated |

### Reviews (sub-resource of Products)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/{id}/reviews/summary` | `GetReviewSummaryQuery` | Public |
| `GET` | `/{id}/reviews` | `GetProductReviewsQuery` | Public (paginated, filterable) |
| `POST` | `/{id}/reviews` | `CreateReviewCommand` | Authenticated |
| `POST` | `/reviews/{reviewId}/vote` | `VoteReviewCommand` | Authenticated |
| `POST` | `/reviews/{reviewId}/response` | `SellerResponseCommand` | Seller |

### Categories (`/api/catalog/categories`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/tree` | `GetCategoryTreeQuery` | Public |
| `GET` | `/` | `ListCategoriesQuery` | Public |
| `POST` | `/` | `CreateCategoryCommand` | Authenticated |
| `PUT` | `/{id}` | `UpdateCategoryCommand` | Authenticated |
| `DELETE` | `/{id}` | `DeleteCategoryCommand` | Authenticated |
| `POST` | `/{id}/attributes` | Direct (AddAttributeDefinition) | Authenticated |
| `GET` | `/{id}/attributes` | Direct (GetAttributeDefinitions) | Public |
| `DELETE` | `/{id}/attributes/{attrId}` | Direct (RemoveAttributeDefinition) | Authenticated |

## Integration Events

### Published (via Outbox)

| Event | Domain Trigger |
|:---|:---|
| `ProductCreatedEvent` | Product.Create() |
| `ProductUpdatedEvent` | Product.Update() / Activate() / Deactivate() |
| `ProductDeletedEvent` | Product.SoftDelete() |
| `SkuCreatedIntegrationEvent` | Product.AddSku() |
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
- ✅ Reviews with seller responses and helpful votes
- ✅ Product recommendations (same category)
