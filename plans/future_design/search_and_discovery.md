# Feature Report: Advanced Search & Discovery

## Inspiration: eBay, Wayfair, Pinterest
Standard keyword search is table stakes. To build a modern marketplace, discovery must cater to both specific intent (searching for a precise model number) and broad intent (browsing for inspiration).

## Key Features to Implement

### 1. Powerful Faceted Filtering
- **Concept:** On the left side of search results, provide deep filtering options dynamically generated based on the category. Filters should include: Price sliders, Brand checkboxes, and category-specific attributes (e.g., Megapixels for cameras, Sleeve length for shirts).
- **Micro-feature:** Show item counts next to each filter (e.g., "Sony (124)"), and update the results instantly without full page reloads.
- **Service Integration:** `Search.API` (Elasticsearch aggregations).

### 2. Visual Search (Image Upload)
- **Concept:** Allow users on mobile (or desktop) to tap a camera icon in the search bar to upload a photo or take a picture of an item to find visually similar products.
- **Service Integration:** A specialized ML service or an integration with Azure Computer Vision, passing results to `Search.API`.

### 3. Dynamic Breadcrumbs
- **Concept:** As users drill down into categories, provide interactive breadcrumbs (Home > Electronics > Laptops > Gaming Laptops) where clicking any node brings up a dropdown of sibling categories.

### 4. "Save Search" and Price Alerts
- **Concept:** Allow users to save a complex search query (e.g., "Used RTX 3080 under $400") and receive push notifications when new items matching the criteria are listed or prices drop.
- **Service Integration:** `Search.API`, `Identity.API` (to save the preference), and `Notification.Worker` (to push the alert via SignalR or Email).