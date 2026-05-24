import { TestBed } from '@angular/core/testing';
import { CartStore } from './cart.store';
import { CartService } from './cart.service';

describe('CartStore', () => {
  let mockCartService: any;
  let store: any;

  const PRODUCT_ID = 'PROD-1';

  beforeEach(() => {
    mockCartService = {
      getCart: vi.fn().mockResolvedValue({ buyerId: 'test-user', cartId: 'cart-1', items: [] }),
      deleteCart: vi.fn().mockResolvedValue(undefined),
      checkout: vi.fn().mockResolvedValue({ correlationId: 'test-correlation-id' }),
      addItem: vi.fn().mockImplementation((productId: string, quantity: number) =>
        Promise.resolve({ buyerId: 'test-user', cartId: 'cart-1', items: [{ productId, storeId: 'store-1', quantity, price: 0, lineTotal: 0 }] })
      ),
      updateItem: vi.fn().mockImplementation((productId, quantity) =>
        Promise.resolve({ buyerId: 'test-user', cartId: 'cart-1', items: [{ productId, storeId: 'store-1', quantity, price: 0, lineTotal: 0 }] })
      ),
      removeItem: vi.fn().mockResolvedValue({ buyerId: 'test-user', cartId: 'cart-1', items: [] }),
      setCartId: vi.fn(),
      getCartId: vi.fn().mockReturnValue(null),
      clearCartId: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: CartService, useValue: mockCartService }
      ]
    });

    store = TestBed.inject(CartStore);
  });

  it('should initialize with an empty cart and load it', async () => {
    expect(mockCartService.getCart).toHaveBeenCalled();
    await Promise.resolve();
    expect(store.items()).toEqual([]);
    expect(store.isEmpty()).toBe(true);
  });

  describe('addToCart', () => {
    it('should add a new item', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', cartId: 'cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 'store-1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      // addToCart calls loadCart() internally to re-fetch enriched cart from BFF
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: 'test-user', cartId: 'cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 'store-1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);
      expect(store.items()).toEqual([{ productId: PRODUCT_ID, storeId: 'store-1', quantity: 1, price: 10, lineTotal: 10 }]);
      expect(mockCartService.addItem).toHaveBeenCalledWith(PRODUCT_ID, 1);
    });
  });

  describe('updateQuantity', () => {
    it('should update quantity', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', cartId: 'cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      mockCartService.updateItem.mockResolvedValueOnce({
        buyerId: 'test-user', cartId: 'cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 5, price: 10, lineTotal: 50 }]
      });
      await store.updateQuantity(PRODUCT_ID, 5);
      expect(mockCartService.updateItem).toHaveBeenCalledWith(PRODUCT_ID, 5);
    });

    it('should remove if quantity is 0', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', cartId: 'cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      mockCartService.removeItem.mockResolvedValueOnce({ buyerId: 'test-user', cartId: 'cart-1', items: [] });
      await store.updateQuantity(PRODUCT_ID, 0);
      expect(store.items()).toEqual([]);
    });
  });

  describe('removeFromCart', () => {
    it('should remove item', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', cartId: 'cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      mockCartService.removeItem.mockResolvedValueOnce({ buyerId: 'test-user', cartId: 'cart-1', items: [] });
      await store.removeFromCart(PRODUCT_ID);
      expect(mockCartService.removeItem).toHaveBeenCalledWith(PRODUCT_ID);
    });
  });

  describe('checkout', () => {
    it('should checkout and clear cart', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', cartId: 'cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);
      await store.checkout();
      expect(store.items()).toEqual([]);
      expect(store.checkoutCorrelationId()).toBe('test-correlation-id');
    });

    it('should clear anonymous cartId on checkout', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      await store.checkout();
      expect(mockCartService.clearCartId).toHaveBeenCalled();
      expect(store.cartId()).toBeNull();
    });
  });

  // ── Anonymous cart flow ──

  describe('anonymous cart flow', () => {
    it('should persist cartId from anonymous getCart response', async () => {
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-cart-999',
        items: [{ productId: 'p1', storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });

      await store.loadCart();
      expect(mockCartService.setCartId).toHaveBeenCalledWith('anon-cart-999');
      expect(store.cartId()).toBe('anon-cart-999');
    });

    it('should NOT persist cartId from authenticated getCart response', async () => {
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: 'user-123', cartId: 'cart-auth',
        items: [{ productId: 'p1', storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });

      await store.loadCart();
      expect(mockCartService.setCartId).not.toHaveBeenCalled();
      expect(store.cartId()).toBe('cart-auth');
    });

    it('should clear anonymous cartId after login (when buyerId is present)', async () => {
      // Simulate: user was anonymous, then logs in, loadCart returns authenticated cart
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: 'user-123', cartId: 'cart-auth',
        items: [{ productId: 'p1', storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });

      await store.loadCart();
      expect(mockCartService.clearCartId).toHaveBeenCalled();
      expect(store.cartId()).toBe('cart-auth');
    });

    it('should persist cartId from anonymous addItem response', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-cart-new',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-cart-new',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });

      await store.addToCart(PRODUCT_ID, 1);
      expect(mockCartService.setCartId).toHaveBeenCalledWith('anon-cart-new');
    });

    it('should add item to anonymous cart and track cartId', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 2, price: 15, lineTotal: 30 }]
      });
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-cart-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 2, price: 15, lineTotal: 30 }]
      });

      await store.addToCart(PRODUCT_ID, 2);
      expect(store.items().length).toBe(1);
      expect(store.items()[0].quantity).toBe(2);
    });

    it('should clear anonymous cart on clearAnonymousCart', () => {
      store.clearAnonymousCart();
      expect(mockCartService.clearCartId).toHaveBeenCalled();
      expect(store.cartId()).toBeNull();
      expect(store.items()).toEqual([]);
    });

    it('should handle empty anonymous cart response', async () => {
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-empty',
        items: []
      });

      await store.loadCart();
      expect(store.isEmpty()).toBe(true);
      expect(store.cartId()).toBe('anon-empty');
    });

    it('should update quantity on anonymous cart', async () => {
      // First add
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      // Update quantity
      mockCartService.updateItem.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 3, price: 10, lineTotal: 30 }]
      });
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 3, price: 10, lineTotal: 30 }]
      });
      await store.updateQuantity(PRODUCT_ID, 3);
      expect(mockCartService.updateItem).toHaveBeenCalledWith(PRODUCT_ID, 3);
    });

    it('should remove item from anonymous cart', async () => {
      // First add
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1',
        items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      // Remove
      mockCartService.removeItem.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1', items: []
      });
      mockCartService.getCart.mockResolvedValueOnce({
        buyerId: null, cartId: 'anon-1', items: []
      });
      await store.removeFromCart(PRODUCT_ID);
      expect(store.isEmpty()).toBe(true);
    });
  });
});
