# Task: Implement Cart Feature Unit Tests (Frontend)

**Goal**: Implement unit tests for the Cart feature, ensuring correct state synchronization between the local store and the Cart API.

**Context**: 
- Framework: Angular 21
- Testing: Vitest
- Location: `src/web/src/app/features/cart/`
- Reference Plans: `7.3.1` through `7.3.3`

**Action Items**:
1. **CartService Tests (`cart.service.spec.ts`)**:
   - Test: `getCart`, `addItem`, `updateQuantity`, `removeItem`.
   - Test: Verify correct HTTP methods (GET, POST, PUT, DELETE) to `/api/cart`.
2. **CartStore Tests (`cart.store.spec.ts`)**:
   - Test: `cartItems` count matches the state.
   - Test: `totalPrice` computed signal correctly sums item prices.
   - Test: `addItem` logic (optimistic update or direct sync).
3. **MiniCartComponent Tests (`mini-cart.spec.ts`)**:
   - Test: Displays item count badge correctly.
   - Test: Shows empty state when no items are present.
4. **CartPage Tests (`cart-page.spec.ts`)**:
   - Test: List all items with correct quantities.
   - Test: "Checkout" button behavior.

**Validation**:
- Run: `cd src/web && pnpm run test --watch=false`
