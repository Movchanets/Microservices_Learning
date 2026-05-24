# Feature Report: Homepage & Global Layout

## Current State Analysis
The current interface (`Screenshot 2026-05-15 235155.png`) looks like a generic dashboard template. It features a persistent left sidebar for categories, a minimalistic header, and a basic card grid. This layout wastes horizontal space, minimizes the importance of search, and doesn't encourage impulse discovery.

## Target State (Rozetka / Prom.ua Patterns)
The homepage should act as a promotional hub and a springboard for search/navigation, rather than just listing products.

### 1. The Global Header
The header is the most critical piece of real estate.
- **Left**: Branding/Logo.
- **Next to Logo**: A prominent "Каталог" (Catalog) button (usually with a hamburger or grid icon) that opens the mega-menu overlay.
- **Center (Huge)**: A wide, persistent search bar powered by `Search.API`. It should feature placeholder text (e.g., "I am looking for...") and a prominent "Search" button. Autocomplete should appear as the user types.
- **Right**: Utility icons (with notification badges):
  - Language toggle (UK/EN).
  - Comparison list (Scales icon).
  - Wishlist (Heart icon).
  - User Profile (Avatar icon) -> opens a small dropdown for quick actions (Sign In, Profile, Orders, Sign Out).
  - Cart (Shopping Cart icon) -> shows item count from `Cart.API`.

### 2. Homepage Content Blocks
Instead of a static grid, the homepage body should be composed of dynamic rows:
- **Hero Section**: A carousel of promotional banners (Sales, New Arrivals, Brand of the week).
- **Recent Views**: "You recently viewed" carousel.
- **Personalized Recommendations**: "Recommended for you" based on previous behavior.
- **Category Tiles**: Visual tiles for popular categories (e.g., Smartphones, Laptops) to drive drill-down navigation.
- **Promotional Grids**: "Deal of the Day" or "Top Sales" with countdown timers.

### 3. Service Integrations
- `Search.API` for the search bar autocomplete.
- `Catalog.API` for populating the category tiles and promotional product lists.
- `Cart.API` for real-time header cart counter updates.