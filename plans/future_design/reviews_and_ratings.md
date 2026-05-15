# Feature Report: Reviews & Ratings

## Inspiration: Amazon, Sephora, Rozetka
User-generated content (UGC) is critical for building trust. A robust review system goes beyond a simple 5-star rating; it provides context, visual proof, and interaction between buyers and sellers.

## Key Features to Implement

### 1. Rich Media Reviews
- **Concept:** Allow users to upload photos and short videos alongside their text review. Visual proof from real buyers significantly increases conversion rates.
- **Micro-feature:** Create a "Customer Photos" gallery at the top of the review section, aggregating all images uploaded by users for that product.
- **Service Integration:** `Catalog.API` (to store the review metadata/text) and `Media.API` (to handle the image/video uploads and CDN delivery).

### 2. Verified Purchase Badges
- **Concept:** Clearly mark reviews from users who actually bought the item on the platform with a "Verified Purchase" badge.
- **Service Integration:** `Ordering.API` (to verify the user ID has a completed order for the product ID) and `Identity.API`.

### 3. Review Filtering and Sorting
- **Concept:** Users shouldn't have to scroll through hundreds of reviews. Provide filters:
  - Sort by: Most Helpful, Newest, Highest Rating, Lowest Rating.
  - Filter by: Star rating (clickable histogram), "With Photos only".
  - Keyword search within reviews (e.g., searching for "battery life" within the reviews of a laptop).
- **Service Integration:** `Search.API` (indexing the review text for fast querying) or `Catalog.API`.

### 4. "Helpful" Voting (Upvoting/Downvoting)
- **Concept:** Allow other users to vote whether a review was helpful or not. The most helpful reviews float to the top.
- **Service Integration:** `Catalog.API` (to track votes per review) and `Identity.API` (to prevent duplicate voting).

### 5. Seller Responses
- **Concept:** Allow verified sellers to publicly reply to reviews (especially negative ones) to show customer service and resolve issues.
- **Service Integration:** `StoreManagement.API` (verifying seller identity) and `Catalog.API`.

### 6. Granular Rating Breakdown (Optional)
- **Concept:** For specific categories (like electronics or clothing), ask users to rate specific aspects (e.g., "Battery Life: 4/5", "Screen Quality: 5/5", or "Fit: Runs Small").
- **Service Integration:** `Catalog.API` (handling dynamic rating attributes per category).

## Placement in the UI
- **Product Detail Page (PDP):** A summary rating (stars and count) at the very top under the product title. The full review section should be placed below the product description, acting as the final social proof before the footer.
- **User Profile:** A dedicated "My Reviews" tab where users can see all reviews they've left and edit/delete them.