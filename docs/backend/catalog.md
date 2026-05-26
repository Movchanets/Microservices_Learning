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
| `Product` | Aggregate Root | Name, Description, Brand, ImageUrl, Tags, Status (Draft/Active/Inactive/Deleted), CategoryId, StoreId |
| `Sku` | Child Entity (of Product) | ProductId, SkuCode, Price (Money VO), Status, TypedAttributes (jsonb), FlexibleAttributes (jsonb) |
| `Category` | Entity | Name, ParentId (tree), AttributeDefinitions |
| `AttributeDefinition` | Entity | Key, DisplayName, Target (Product/Sku), ValueType, IsFilterable, IsRequired, AllowedValues |
| `Review` | Aggregate Root | ProductId, UserId, Rating, Title, Body, Photos, SellerResponse |
| `ReviewVote` | Entity | ReviewId, UserId, IsHelpful |

### SKU Refactor Status

The SKU refactoring (Phases 1–8) is **complete in the domain model**. SKUs are child entities of Product carrying their own Price, TypedAttributes, and FlexibleAttributes.

**Domain events now raised:**
- `ProductCreatedDomainEvent` → `ProductCreatedEvent` ✅
- `ProductUpdatedDomainEvent` → `ProductUpdatedEvent` ✅
- `ProductDeletedDomainEvent` → `ProductDeletedEvent` ✅
- `SkuCreatedDomainEvent` → `SkuCreatedIntegrationEvent` ✅
- `SkuDeletedDomainEvent` → `SkuDeletedEvent` ✅ (handler created)
- `SkuPriceChangedDomainEvent` → `SkuPriceChangedEvent` ✅ (handler created)

**Price changes** now go through `Product.ChangeSkuPrice()` which captures old price before mutation and fires `SkuPriceChangedDomainEvent`.

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

### Legacy (backward-compat fields)

`ProductCreatedEvent` and `ProductUpdatedEvent` carry `Price=0`, `Sku=""` backward-compat fields. **These should be removed** once all consumers are migrated to SKU-level events.

## Current Status & Known Issues

- ✅ SKU refactor complete in domain model with all 6 domain events
- ✅ All event handlers created (SkuDeleted, SkuPriceChanged)
- 🔴 **Seeder AddSku returns 409** — `AddSkuValidator` regex or `ValidateRequiredAttributes()` throws `InvalidOperationException` mapped to 409 by GlobalExceptionMiddleware. Blocks product activation.
- 🟡 Backward-compat fields on ProductCreatedEvent/ProductUpdatedEvent still present
- 🟡 `Product.ChangeSkuPrice()` correctly captures old price before mutation
