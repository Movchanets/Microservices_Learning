// CartStore unit tests.
// Verifies the NgRx SignalStore for cart functionality: loadCart, addToCart,
// updateQuantity, removeFromCart, checkout, and computed signals (totalItems,
// isEmpty, totalPrice). Uses mocked CartService.

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
      checkout: vi.fn().mockResolvedValue({ correlationId: 'test-correlation-id' }),
      addItem: vi.fn().mockImplementation((sku: string, quantity: number) =>
        Promise.resolve({ buyerId: 'test-user', items: [{ sku, quantity, unitPrice: 0 }] })
      ),
      updateItem: vi.fn().mockImplementation((sku, quantity) =>
        Promise.resolve({ buyerId: 'test-user', items: [{ sku, quantity, unitPrice: 0 }] })
      ),
      removeItem: vi.fn().mockResolvedValue({ buyerId: 'test-user', items: [] }),
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
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1, unitPrice: 10 }]
      });

      await store.addToCart('PROD-1', 1);

      expect(store.items()).toEqual([{ sku: 'PROD-1', quantity: 1, unitPrice: 10 }]);
      expect(store.isEmpty()).toBe(false);
      expect(store.totalItems()).toBe(1);
      expect(mockCartService.addItem).toHaveBeenCalledWith('PROD-1', 1);
    });

    it('should increase quantity if item already exists', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1, unitPrice: 10 }]
      });
      await store.addToCart('PROD-1', 1);

      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 3, unitPrice: 10 }]
      });
      await store.addToCart('PROD-1', 2);

      expect(store.items()).toEqual([{ sku: 'PROD-1', quantity: 3, unitPrice: 10 }]);
      expect(store.totalItems()).toBe(3);
    });
  });

  describe('updateQuantity', () => {
    it('should update the quantity of an existing item', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1, unitPrice: 10 }]
      });
      await store.addToCart('PROD-1', 1);

      mockCartService.updateItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 5, unitPrice: 10 }]
      });
      await store.updateQuantity('PROD-1', 5);

      expect(store.items()).toEqual([{ sku: 'PROD-1', quantity: 5, unitPrice: 10 }]);
      expect(mockCartService.updateItem).toHaveBeenCalledWith('PROD-1', 5);
    });

    it('should remove the item if quantity is 0', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1, unitPrice: 10 }]
      });
      await store.addToCart('PROD-1', 1);

      mockCartService.removeItem.mockResolvedValueOnce({
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
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1, unitPrice: 10 }]
      });
      await store.addToCart('PROD-1', 1);

      mockCartService.removeItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: []
      });
      await store.removeFromCart('PROD-1');

      expect(store.items()).toEqual([]);
      expect(store.isEmpty()).toBe(true);
      expect(mockCartService.removeItem).toHaveBeenCalledWith('PROD-1');
    });
  });

  describe('totalPrice', () => {
    it('should correctly sum item prices', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 2, unitPrice: 10.5 }]
      });
      await store.addToCart('PROD-1', 2);

      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [
          { sku: 'PROD-1', quantity: 2, unitPrice: 10.5 },
          { sku: 'PROD-2', quantity: 1, unitPrice: 5.0 }
        ]
      });
      await store.addToCart('PROD-2', 1);

      expect(store.totalPrice()).toBe(26);
    });
  });

  describe('checkout', () => {
    it('should call checkout and clear the cart', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
        items: [{ sku: 'PROD-1', quantity: 1, unitPrice: 10 }]
      });
      await store.addToCart('PROD-1', 1);

      await store.checkout();

      expect(mockCartService.checkout).toHaveBeenCalled();
      expect(store.items()).toEqual([]);
      expect(store.checkoutCorrelationId()).toBe('test-correlation-id');
    });
  });
});
