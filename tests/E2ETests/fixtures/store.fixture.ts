// Re-export from checkout.fixture for backward compatibility.
// New tests should import from '../fixtures/checkout.fixture' directly.
export { checkoutTest as storeTest, type CheckoutFixtures as StoreFixtures, expect } from './checkout.fixture';
