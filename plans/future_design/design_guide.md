# Marketplace UI/UX Design Guide

## Executive Summary
This design guide outlines the transformation of the current generic "Marketplace" frontend into a robust, high-conversion e-commerce platform inspired by Ukrainian market leaders like Rozetka and Prom.ua. The goal is to move away from a basic sidebar-and-grid layout to a feature-rich, scalable interface that properly exposes our underlying microservices (Search, Catalog, Ordering, Identity).

## Core Design Principles
1. **Search-First Discovery (Prom/Rozetka model):** The search bar must be the focal point of the header, powered by `Search.API` (Elasticsearch).
2. **Hidden until Hover/Click Menus:** The category sidebar takes up too much permanent screen real estate. It will be replaced by a mega-menu triggered by a "Catalog" button.
3. **Comprehensive User Hub:** The user profile must serve as a command center for orders, wishlists, and settings, mapping to `Ordering.API`, `Identity.API`, and `StoreManagement.API`.
4. **Real-time Feedback:** Leverage `Notification.Worker` (SignalR) to provide instant feedback on order status changes, messages, and cart updates.

## Service to UI Mapping
- **`Search.API`**: Main header search bar with autocomplete, faceted sidebar on search results.
- **`Catalog.API`**: Mega-menu structure, product detail pages.
- **`Cart.API`**: Header cart counter, slide-out cart drawer or dedicated page.
- **`Identity.API` & `StoreManagement.API`**: User profile sidebar, role-based conditional UI (Buyer vs Seller).
- **`Ordering.API`**: User profile "Orders" tab, checkout flow.
- **`Inventory.API`**: Product cards showing "In Stock", "Out of Stock", or "Hurry, 2 left!".

## Future Roadmap
- Phase 1: Implement Global Header (Search + Catalog Button) & Footer.
- Phase 2: Refactor User Profile into a multi-tab dashboard.
- Phase 3: Update Homepage with promotional banners, personalized recommendations, and dynamic category blocks.
- Phase 4: Enhance Product Pages with reviews, rich media (`Media.API`), and seller info.

*See individual feature reports in this directory for detailed breakdowns.*