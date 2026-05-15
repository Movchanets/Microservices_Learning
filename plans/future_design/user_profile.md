# Feature Report: User Profile Hub

## Current State Analysis
The current user profile (`Screenshot 2026-05-15 235507.png`) uses a simple dropdown showing basic roles and sign-out functionality. The actual dashboard seems generic. For a marketplace, the user profile needs to be a comprehensive portal.

## Target State (Rozetka / Prom.ua Patterns)
The reference image (`Screenshot 2026-05-15 235507.png`) shows a dedicated "Personal Account" layout. It features a persistent left navigation specifically for user settings, distinct from the global catalog.

### 1. Profile Navigation Sidebar
When a user navigates to their profile, the left side of the screen becomes a vertical menu with the following essential tabs:
- **User Info Block**: User's name, email, and current loyalty/subscription status (e.g., "Rozetka Premium").
- **Orders (Замовлення)**: The default and most important tab. Lists current and past orders with status badges.
- **Messages/Chats (Листування з продавцями)**: Crucial for marketplace models where buyers communicate with third-party sellers.
- **Personal Offers/Promos**: Targeted discounts.
- **Wishlists & Comparisons**: Easy access to saved items.
- **Reviews (Відгуки)**: History of reviews left by the user.
- **Viewed Products**: History of browsing.
- **Wallet/Bonuses**: Loyalty points balance.
- **Settings**: Address book, payment methods, password reset.

### 2. The "Orders" Tab Experience
As the primary destination:
- Contains an internal search bar ("Search by order number or item").
- Status filters ("Active", "Completed", "Cancelled").
- Each order card displays: Order number, Date, Total amount, Visual status badge (e.g., "Processing", "Shipped", "Delivered"), and thumbnails of the items.
- Expandable to see shipping details, tracking numbers, and seller info.

### 3. Real-time Integration
- The profile sidebar should show unread notification badges (e.g., a red '3' next to Messages or Orders if there's an update).
- Uses `Notification.Worker` (SignalR) to push real-time status updates so the user doesn't have to refresh to see if their order shipped.

### 4. Service Integrations
- **`Identity.API`**: Basic user info, settings, password management.
- **`Ordering.API`**: Fetching order history, statuses, and order details.
- **`StoreManagement.API`**: Fetching seller details for the messaging interface.
- **`Payment.API`**: Wallet, saved cards, and transaction history.