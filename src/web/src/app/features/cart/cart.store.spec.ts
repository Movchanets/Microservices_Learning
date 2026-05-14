import { TestBed } from '@angular/core/testing';
import { CartStore } from './cart.store';
import { CartService } from './cart.service';

describe('CartStore', () => {
  let mockCartService: any;
  let store: any;

  beforeEach(() => {
    mockCartService = {
      getCart: vi.fn().mockResolvedValue({ buyerId: 'test-user', items: [] }),
      updateCart: vi.fn().mockImplementation((cart) => Promise.resolve(cart)),
      deleteCart: vi.fn().mockResolvedValue(undefined),
      checkout: vi.fn().mockResolvedValue({ correlationId: 'test-correlation-id' })
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: CartService, useValue: mockCartService }
      ]
    });

    store = TestBed.inject(CartStore);
  });

  it('should initialize with an empty cart and load it', async () => {
    // onInit calls loadCart
    expect(mockCartService.getCart).toHaveBeenCalled();
    // Wait for the async loadCart to resolve
    await Promise.resolve();

    expect(store.items()).toEqual([]);
    expect(store.isEmpty()).toBe(true);
    expect(store.totalItems()).toBe(0);
  });

  describe('addToCart', () => {
    it('should add a new item to the cart', async () => {
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1 }]
      });

      await store.addToCart('PROD-1', 1);

      expect(store.items()).toEqual([{ sku: 'PROD-1', quantity: 1 }]);
      expect(store.isEmpty()).toBe(false);
      expect(store.totalItems()).toBe(1);
      expect(mockCartService.updateCart).toHaveBeenCalled();
    });

    it('should increase quantity if item already exists', async () => {
      // Setup initial state with an item
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1 }]
      });
      await store.addToCart('PROD-1', 1);

      // Add the same item again
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 3 }]
      });
      await store.addToCart('PROD-1', 2);

      expect(store.items()).toEqual([{ sku: 'PROD-1', quantity: 3 }]);
      expect(store.totalItems()).toBe(3);
    });
  });

  describe('updateQuantity', () => {
    it('should update the quantity of an existing item', async () => {
      // Setup
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1 }]
      });
      await store.addToCart('PROD-1', 1);

      // Update quantity
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 5 }]
      });
      await store.updateQuantity('PROD-1', 5);

      expect(store.items()).toEqual([{ sku: 'PROD-1', quantity: 5 }]);
    });

    it('should remove the item if quantity is 0', async () => {
      // Setup
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1 }]
      });
      await store.addToCart('PROD-1', 1);

      // Update quantity to 0
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: []
      });
      await store.updateQuantity('PROD-1', 0);

      expect(store.items()).toEqual([]);
      expect(store.isEmpty()).toBe(true);
    });
  });

  describe('removeFromCart', () => {
    it('should remove the item from the cart', async () => {
      // Setup
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1 }]
      });
      await store.addToCart('PROD-1', 1);

      // Remove item
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: []
      });
      await store.removeFromCart('PROD-1');

      expect(store.items()).toEqual([]);
      expect(store.isEmpty()).toBe(true);
    });
  });

  describe('totalPrice', () => {
    it('should correctly sum item prices', async () => {
      // Setup mock
      mockCartService.updateCart.mockImplementation((cart: any) => Promise.resolve(cart));

      await store.addToCart('PROD-1', 2, 10.5); // 2 * 10.5 = 21
      await store.addToCart('PROD-2', 1, 5.0);  // 1 * 5 = 5

      expect(store.totalPrice()).toBe(26);
    });
  });

  describe('checkout', () => {
    it('should call checkout and clear the cart', async () => {
      // Setup
      mockCartService.updateCart.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1 }]
      });
      await store.addToCart('PROD-1', 1);

      // Checkout
      await store.checkout();

      expect(mockCartService.checkout).toHaveBeenCalled();
      expect(store.items()).toEqual([]);
      expect(store.checkoutCorrelationId()).toBe('test-correlation-id');
    });
  });
});
