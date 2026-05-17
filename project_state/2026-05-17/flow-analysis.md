# Flow Analysis — 2026-05-17

## Objective analysis of key marketplace flows

**Changes since 2026-05-16:** Flows 3, 5, and 6 updated to reflect audit fixes.

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
4. CartStore calls CartService — now uses single-item endpoint
5. POST /api/cart/items with {sku, quantity, price}
6. Cart.API adds item to existing cart

### Current Implementation
- CartStore.addToCart() does optimistic update + single-item API call
- If API fails, reverts by calling loadCart()
- Single-item endpoints now available (POST /items, PUT /items/{sku}, DELETE /items/{sku})

### Verdict: ⚠️ Functional, improved since 2026-05-16
- ✅ Single-item endpoints added (no more full cart replacement)
- ✅ x-buyer-id header pattern removed
- **Missing:** "Add to Cart" UI button on product detail

---

## Flow 4: Remove from Cart

### Steps
1. Buyer views cart at /cart
2. Clicks remove button on item
3. CartStore.removeFromCart(sku)
4. Filters out item locally
5. Calls DELETE /api/cart/items/{sku}
6. Cart.API removes single item

### Verdict: ✅ Working — improved
- Remove now uses dedicated single-item endpoint
- No longer sends entire cart for single item removal

---

## Flow 5: Checkout

### Steps
1. Buyer views cart → clicks "Checkout"
2. CheckoutStore.submitCheckout()
3. Calls CartStore.checkout()
4. CartService.checkout() → POST /api/cart/checkout
   - **NEW:** Now forwards address fields (AddressLine1, City, State, PostalCode, Country)
5. Cart.API publishes OrderSubmittedEvent (via MassTransit Outbox)
   - **NEW:** Event includes shipping address
6. Ordering Saga starts:
   - Creates order with address
   - Reserves inventory (Inventory.API)
   - Processes payment (Payment.API)
   - **NEW:** 4 projection consumers keep persisted Order in sync
   - Publishes OrderCompletedEvent or OrderCancelledEvent
7. Notification.Worker broadcasts via SignalR
   - **FIXED:** Buyer targeting now uses query string (`?buyerId=`)
8. Frontend receives order update
   - **FIXED:** SignalR starts on login/register/checkAuth (not just app boot)

### Verdict: ✅ Working — significantly improved
- Full saga orchestration with compensation
- Outbox pattern for reliable delivery
- Address forwarding now works end-to-end
- Order read model stays in sync with saga state
- SignalR buyer targeting works in real browsers
- **Missing:** Address form UI, payment method selection

---

## Flow 6: Order Management (Buyer)

### Steps
1. Buyer navigates to /orders
2. OrderStore.loadOrders() → GET /api/orders/buyer/{buyerId}
3. Displays order list with status
4. Click order → OrderDetailComponent
5. Shows order timeline with steps
   - **FIXED:** Order status now accurately reflects saga state (Submitted → InventoryReserved → PaymentProcessing → Completed/Cancelled)

### Verdict: ✅ Working — improved
- Order history available
- Timeline visualization
- **FIXED:** Order status no longer stuck at "Submitted" when saga has advanced
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
- **Residual gap:** Seller order correlation is weak — `OrderItem.SellerId` not reliably propagated during checkout
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
- **Note:** Search.IntegrationTests all fail (6 tests) — Elasticsearch not available in test environment

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

| Flow | Status | Changes since 2026-05-16 |
|------|--------|--------------------------|
| Create Store | ⚠️ Partial | No change |
| Create Product | ✅ Working | No change |
| Add to Cart | ✅ Improved | Single-item endpoints added |
| Remove from Cart | ✅ Improved | Single-item endpoint |
| Checkout | ✅ Improved | Address forwarding, order projection sync, SignalR fix |
| Buyer Orders | ✅ Improved | Status accurately reflects saga state |
| Seller Orders | ✅ Working | No change (correlation gap remains) |
| Admin Verification | ✅ Working | No change |
| Search | ✅ Working | No change |
| Profile | ⚠️ Read-only | No change |

## Architecture Issues Found

1. **Search.API** — No authentication/authorization at all
2. **Notification.Worker** — No authentication on SignalR hub (query string fix helps, but no auth middleware)
3. **SellerOrdersComponent** — Bypasses store pattern (direct HttpClient)
4. **StoreService.getSalesSummary()** — Returns hardcoded zeros
5. **Search.IntegrationTests** — All 6 tests fail (Elasticsearch not running in test env)
