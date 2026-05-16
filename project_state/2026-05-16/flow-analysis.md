# Flow Analysis — 2026-05-16

## Objective analysis of key marketplace flows

---

## Flow 1: Create Store

### Steps
1. User registers as Buyer (POST /api/identity/auth/register)
2. User navigates to /seller (roleGuard blocks — needs Seller role)
3. **GAP:** No way for user to become Seller without admin action
4. Admin verifies store → StoreVerifiedEvent → Identity updates role to Seller
5. **But:** User can't create a store without being a Seller first (circular dependency)

### Current Workaround
- Admin manually updates user role to Seller via admin panel
- Then user can create store

### Verdict: ⚠️ Partially working
- Store creation endpoint exists and works
- But the flow has a chicken-and-egg problem: need Seller role to create store, but role comes from store verification
- **Missing:** "Apply to become seller" flow or self-service store creation for Buyers

---

## Flow 2: Create Product

### Steps
1. Seller logs in → navigates to /seller
2. Clicks "Add Product" → SellerProductFormComponent
3. Fills in: name, description, price, categoryId, sku
4. Submits → POST /api/catalog/products (with storeId from localStorage)
5. Product created in Catalog DB
6. MassTransit publishes ProductCreatedEvent
7. Search.IndexingService consumes event → indexes in Elasticsearch
8. Product appears in catalog search

### Verdict: ✅ Working
- Full CRUD available (create, read, update, delete)
- Price change via PATCH endpoint
- Search indexing via events
- **Minor:** storeId stored in localStorage (not ideal)

---

## Flow 3: Add to Cart

### Steps
1. Buyer browses catalog → finds product
2. **GAP:** No "Add to Cart" button on product detail page
3. If button existed: CartStore.addToCart(sku, quantity, price)
4. CartStore calls CartService.updateCart() — **full cart replacement**
5. POST /api/cart with all items (not single item add)
6. Cart.API replaces entire cart

### Current Implementation
- CartStore.addToCart() does optimistic update + full replacement
- If API fails, reverts by calling loadCart()
- CartService still has x-buyer-id header pattern (legacy, should use JWT claims only)

### Verdict: ⚠️ Functional but suboptimal
- Works via full cart replacement
- No dedicated single-item endpoint (TODO in CartEndpoints.cs)
- Optimistic update with fallback is reasonable
- **Missing:** "Add to Cart" UI button on product detail
- **Missing:** Single-item endpoints (POST /items, PUT /items/{sku}, DELETE /items/{sku})

---

## Flow 4: Remove from Cart

### Steps
1. Buyer views cart at /cart
2. Clicks remove button on item
3. CartStore.removeFromCart(sku)
4. Filters out item locally
5. Calls CartService.updateCart() with remaining items
6. POST /api/cart — full replacement

### Verdict: ✅ Working
- Remove works via full cart replacement
- Quantity update also works
- **Minor:** Inefficient (sends entire cart for single item removal)

---

## Flow 5: Checkout

### Steps
1. Buyer views cart → clicks "Checkout"
2. CheckoutStore.submitCheckout()
3. Calls CartStore.checkout()
4. CartService.checkout() → POST /api/cart/checkout
5. Cart.API publishes OrderSubmittedEvent (via MassTransit Outbox)
6. Ordering Saga starts:
   - Creates order
   - Reserves inventory (Inventory.API)
   - Processes payment (Payment.API)
   - Publishes OrderCreatedEvent
7. Notification.Worker broadcasts via SignalR
8. Frontend receives order update

### Verdict: ✅ Working
- Full saga orchestration with compensation
- Outbox pattern for reliable delivery
- **Missing:** Address form, payment method selection

---

## Flow 6: Order Management (Buyer)

### Steps
1. Buyer navigates to /orders
2. OrderStore.loadOrders() → GET /api/orders/buyer/{buyerId}
3. Displays order list with status
4. Click order → OrderDetailComponent
5. Shows order timeline with steps

### Verdict: ✅ Working
- Order history available
- Timeline visualization
- **Missing:** Order cancellation UI

---

## Flow 7: Order Management (Seller)

### Steps
1. Seller navigates to /seller → Orders tab
2. SellerOrdersComponent loads orders
3. GET /api/orders/seller/{sellerId}
4. Displays orders containing seller's products

### Verdict: ✅ Working
- Seller can view their orders
- **Missing:** Order status update (ship, complete)

---

## Flow 8: Admin Store Verification

### Steps
1. Admin navigates to /admin → Pending Stores tab
2. AdminStore.loadPendingStores()
3. GET /api/stores?status=Pending
4. Admin reviews store details
5. Clicks "Approve" or "Reject"
6. POST /api/stores/{id}/verify with {isApproved, reason}
7. StoreManagement publishes StoreVerifiedEvent
8. Identity.StoreVerifiedConsumer updates user role to Seller
9. User can now access seller features

### Verdict: ✅ Working
- Full verification pipeline
- Event-driven role update
- **Works end-to-end**

---

## Flow 9: Search Products

### Steps
1. User visits /catalog
2. Types search query (debounced 350ms)
3. CatalogStore.searchQuery updates
4. GET /api/search/products?q=query
5. Elasticsearch returns results with facets
6. Displayed in product grid with pagination

### Verdict: ✅ Working
- Full-text search via Elasticsearch
- Category and price filters
- Pagination supported
- **Note:** Search.API has NO authentication (no UseAuthentication/UseAuthorization)

---

## Flow 10: User Profile

### Steps
1. User navigates to /profile
2. ProfileComponent displays user info from AuthStore
3. **GAP:** No edit functionality
4. **GAP:** No change password

### Verdict: ⚠️ Read-only
- Can view profile
- Cannot edit or change password

---

## Summary

| Flow | Status | Notes |
|------|--------|-------|
| Create Store | ⚠️ Partial | Circular dependency with role |
| Create Product | ✅ Working | Full CRUD, search indexing via events |
| Add to Cart | ⚠️ Suboptimal | Full replacement, no single-item endpoint |
| Remove from Cart | ✅ Working | Via full replacement |
| Checkout | ✅ Working | Saga orchestration, polling for order |
| Buyer Orders | ✅ Working | History + timeline visualization |
| Seller Orders | ✅ Working | View only, no status update |
| Admin Verification | ✅ Working | Event-driven role update |
| Search | ✅ Working | Elasticsearch, no auth on API |
| Profile | ⚠️ Read-only | No edit/change password |

## Architecture Issues Found

1. **Search.API** — No authentication/authorization at all
2. **Notification.Worker** — No authentication on SignalR hub
3. **CartService** — Legacy x-buyer-id header pattern (should use JWT claims)
4. **SellerOrdersComponent** — Bypasses store pattern (direct HttpClient)
5. **StoreService.getSalesSummary()** — Returns hardcoded zeros
