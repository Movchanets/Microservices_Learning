# Feature Report: Product Detail Page (PDP)

## Inspiration: Amazon, AliExpress, Taobao
The Product Detail Page is where conversion happens. Moving away from a generic card layout, the PDP must provide overwhelming proof, clear variations, and frictionless purchasing.

## Key Features to Implement

### 1. Sticky Buy Box (Desktop & Mobile)
- **Concept:** As the user scrolls down to read long descriptions or reviews, the "Add to Cart" and price stay pinned to the top (or bottom on mobile) of the screen.
- **Service Integration:** `Cart.API` and `Inventory.API` (to ensure the item hasn't gone out of stock while reading).

### 2. "Frequently Bought Together" Bundles
- **Concept:** Show a bundle of 2-3 items (e.g., Camera + Memory Card + Case) with a single "Add all 3 to Cart" button.
- **Service Integration:** `Catalog.API` or a future `Recommendation.API` based on association rules.

### 3. Advanced Product Variations Selector
- **Concept:** Instead of simple dropdowns, use visual swatches for colors and clear pill buttons for sizes. If a specific color/size combo is out of stock, cross it out visually but allow the user to click it to see an "Email me when available" button.
- **Service Integration:** `Catalog.API` (variants) and `Inventory.API` (stock per variant).

### 4. Community Q&A and Rich Reviews
- **Concept:** Allow users to ask questions that other buyers or the seller can answer. Reviews should include customer-uploaded photos, pros/cons bullet points, and a rating distribution chart (e.g., % of 5 stars vs 1 star).
- **Service Integration:** `StoreManagement.API` (seller responses), `Media.API` (review photos).

### 5. Scarcity & Urgency Indicators
- **Concept:** "Only 3 left in stock - order soon" or "Order within 2 hrs 14 mins to get it by tomorrow".
- **Service Integration:** `Inventory.API` and shipping calculation logic.