# Plan 05: Reviews & Ratings System

## Goal
Implement a complete review system with star ratings, text reviews, photo uploads, verified purchase badges, and helpful voting.

## Context
- **Current state:** No review system exists. Product detail page has no reviews section.
- **Target state:** Amazon/Rozetka-style reviews with rich media, filtering, sorting, and seller responses.
- **Design ref:** `plans/future_design/reviews_and_ratings.md`
- **Services involved:** Catalog.API (review storage), Media.API (photo uploads), Ordering.API (verified purchase check)

## Prerequisites
- Catalog.API has product CRUD — exists
- Media.API has file upload — exists
- Ordering.API has GET /api/orders/buyer/{buyerId} — exists

## Backend Changes

### 1. Create Review Domain Model
**New file:** `src/Microservices/Catalog/Catalog.Domain/Aggregates/Review.cs`

```csharp
public class Review : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; }
    public int Rating { get; private set; } // 1-5
    public string Title { get; private set; }
    public string Text { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public List<string> PhotoUrls { get; private set; } = [];
    public int HelpfulCount { get; private set; }
    public int NotHelpfulCount { get; private set; }
    public string? SellerResponse { get; private set; }
    public DateTime? SellerResponseDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

### 2. Add Review Entity Configuration
**New file:** `src/Microservices/Catalog/Catalog.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs`

EF Core configuration for Review entity. Store PhotoUrls as JSON array.

### 3. Add Review Endpoints
**File:** `src/Microservices/Catalog/Catalog.API/Endpoints/ProductEndpoints.cs`

```csharp
// Get reviews for product
group.MapGet("/{id:guid}/reviews", async (Guid id, ...) => { ... });

// Add review
group.MapPost("/{id:guid}/reviews", async (Guid id, CreateReviewCommand cmd, ...) => { ... })
    .RequireAuthorization();

// Vote helpful
group.MapPost("/reviews/{reviewId}/vote", async (Guid reviewId, VoteReviewCommand cmd, ...) => { ... })
    .RequireAuthorization();

// Seller response
group.MapPost("/reviews/{reviewId}/response", async (Guid reviewId, SellerResponseCommand cmd, ...) => { ... })
    .RequireAuthorization("Seller");
```

### 4. Create Review Commands & Queries
**New files:**
- `Catalog.Application/Commands/CreateReview/CreateReviewCommand.cs` + Handler + Validator
- `Catalog.Application/Commands/VoteReview/VoteReviewCommand.cs` + Handler
- `Catalog.Application/Commands/SellerResponse/SellerResponseCommand.cs` + Handler
- `Catalog.Application/Queries/GetProductReviews/GetProductReviewsQuery.cs` + Handler

CreateReviewHandler should:
1. Validate user hasn't already reviewed this product
2. Check Ordering.API for verified purchase (optional, can be async)
3. Create Review entity
4. Save to DB

### 5. Add Review DTOs
**New file:** `src/Microservices/Catalog/Catalog.Application/DTOs/ReviewDto.cs`

```csharp
public record ReviewDto(
    Guid Id,
    Guid UserId,
    string UserName,
    int Rating,
    string Title,
    string Text,
    bool IsVerifiedPurchase,
    List<string> PhotoUrls,
    int HelpfulCount,
    int NotHelpfulCount,
    string? SellerResponse,
    DateTime? SellerResponseDate,
    DateTime CreatedAt);

public record ReviewSummaryDto(
    double AverageRating,
    int TotalReviews,
    Dictionary<int, int> RatingDistribution); // star -> count
```

### 6. Add Review Migration
Run `dotnet ef migrations add AddReviews --project src/Microservices/Catalog/Catalog.Infrastructure`

## Frontend Changes

### 7. Create Review Service
**New file:** `src/web/src/app/features/catalog/review.service.ts`

```typescript
getReviews(productId: string, page: number, sort: string): Promise<PagedResult<Review>>
getReviewSummary(productId: string): Promise<ReviewSummary>
submitReview(productId: string, review: CreateReviewRequest): Promise<Review>
voteReview(reviewId: string, helpful: boolean): Promise<void>
```

### 8. Create Review Store
**New file:** `src/web/src/app/features/catalog/review.store.ts`

State: reviews, summary, loading, filters (sort, rating filter, photo-only filter)

### 9. Create Review Components

**New files:**
- `src/web/src/app/features/catalog/components/review-summary/review-summary.ts`
  - Average rating (large stars)
  - Rating distribution histogram (5 star bar chart)
  - Total review count

- `src/web/src/app/features/catalog/components/review-list/review-list.ts`
  - Sort dropdown (Most Helpful, Newest, Highest, Lowest)
  - Filter: star rating buttons, "With Photos" toggle
  - List of review cards

- `src/web/src/app/features/catalog/components/review-card/review-card.ts`
  - Stars + title + text
  - Photo gallery (clickable thumbnails)
  - Verified Purchase badge
  - Helpful/Not Helpful buttons with count
  - Seller response (if any)
  - Date

- `src/web/src/app/features/catalog/components/write-review/write-review.ts`
  - Star selector (clickable)
  - Title input
  - Text textarea
  - Photo upload (max 5 images, uses Media.API)
  - Submit button

### 10. Add Reviews to Product Detail
**File:** `src/web/src/app/features/catalog/product-detail/product-detail.ts`

Add below product description:
1. Review summary (average + histogram)
2. Write review button (if authenticated + purchased)
3. Review list with filters

### 11. Upload Photos via Media.API
Use existing `POST /api/media/upload` endpoint. Store returned blob URLs in review.

## Files to Modify/Create

| Action | File |
|--------|------|
| CREATE | `Catalog.Domain/Aggregates/Review.cs` |
| CREATE | `Catalog.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs` |
| MODIFY | `Catalog.Infrastructure/Persistence/CatalogDbContext.cs` (add DbSet<Review>) |
| CREATE | `Catalog.Application/Commands/CreateReview/` (Command, Handler, Validator) |
| CREATE | `Catalog.Application/Commands/VoteReview/` (Command, Handler) |
| CREATE | `Catalog.Application/Commands/SellerResponse/` (Command, Handler) |
| CREATE | `Catalog.Application/Queries/GetProductReviews/` (Query, Handler) |
| CREATE | `Catalog.Application/DTOs/ReviewDto.cs` |
| MODIFY | `Catalog.API/Endpoints/ProductEndpoints.cs` |
| CREATE | EF Migration |
| CREATE | `src/web/src/app/features/catalog/review.service.ts` |
| CREATE | `src/web/src/app/features/catalog/review.store.ts` |
| CREATE | `src/web/src/app/features/catalog/components/review-summary/review-summary.ts` |
| CREATE | `src/web/src/app/features/catalog/components/review-list/review-list.ts` |
| CREATE | `src/web/src/app/features/catalog/components/review-card/review-card.ts` |
| CREATE | `src/web/src/app/features/catalog/components/write-review/write-review.ts` |
| MODIFY | `src/web/src/app/features/catalog/product-detail/product-detail.ts` |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. `dotnet test tests/UnitTests/Catalog.UnitTests/` — passes
4. Manual: Product detail → reviews section visible
5. Manual: Write review → star selector, text, photo upload
6. Manual: Submit review → appears in list
7. Manual: Vote helpful → count updates
8. Manual: Verified purchase badge shows for buyers
9. Manual: Filter by rating, sort by helpful/newest
