# Feature Report: Catalog Navigation

## Current State Analysis
Currently, categories are listed in a persistent left-hand sidebar ("Electronics, Home & Kitchen", etc.). This is a limiting pattern for a marketplace with thousands of categories, as it clutters the UI and cannot display deep hierarchies effectively.

## Target State (Rozetka / Prom.ua Patterns)
Following the pattern shown in the reference image (`Screenshot 2026-05-15 235544.png`), the catalog should be hidden behind a global button and revealed as an expansive mega-menu overlay.

### 1. The Catalog Button
- Positioned in the global header, next to the logo.
- Visually distinct (e.g., dark background or prominent border) to draw the eye.
- Label: "Каталог" or "Catalog" with an icon (hamburger menu or 3x3 dots grid).

### 2. The Mega-Menu Overlay
When the Catalog button is clicked (or hovered, depending on UX preference), a full-width or large overlay drops down over the page content.
- **Left Column (Root Categories)**: A vertical list of top-level categories (e.g., Computers, Home Appliances, Clothing) with distinct icons. Hovering over one of these changes the content in the right pane.
- **Right Content Area (Subcategories)**: When a root category is hovered, this area populates with columns of subcategories and leaf nodes (e.g., Laptops -> Asus, Apple, Lenovo; PC Components -> Processors, SSDs).
- **Promotional Space (Optional)**: The far right of the mega-menu can contain banners for current sales within the hovered category.

### 3. Benefits of this Pattern
- **Space Efficiency**: Frees up the entire homepage and search result pages to display products and filters.
- **Hierarchy Visibility**: Allows users to jump straight to a 3rd-level category (e.g., "Gaming Mice") without clicking through multiple pages.
- **Scalability**: Can handle marketplaces with massive, diverse inventories (Prom/Rozetka scale).

### 4. Service Integrations
- Powered primarily by `Catalog.API`.
- The frontend should fetch the category tree (up to level 3) on application load and cache it aggressively to ensure the mega-menu opens instantly without latency.