import { TestBed } from '@angular/core/testing';
import { CartStore } from './cart.store';
import { CartService } from './cart.service';

describe('CartStore', () => {
  let mockCartService: any;
  let store: any;

  const PRODUCT_ID = 'PROD-1';

  beforeEach(() => {
    mockCartService = {
      getCart: vi.fn().mockResolvedValue({ buyerId: 'test-user', items: [] }),
      deleteCart: vi.fn().mockResolvedValue(undefined),
      checkout: vi.fn().mockResolvedValue({ correlationId: 'test-correlation-id' }),
      addItem: vi.fn().mockImplementation((productId: string, quantity: number) =>
        Promise.resolve({ buyerId: 'test-user', items: [{ productId, storeId: 'store-1', quantity, price: 0, lineTotal: 0 }] })
      ),
      updateItem: vi.fn().mockImplementation((productId, quantity) =>
        Promise.resolve({ buyerId: 'test-user', items: [{ productId, storeId: 'store-1', quantity, price: 0, lineTotal: 0 }] })
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
    expect(mockCartService.getCart).toHaveBeenCalled();
    await Promise.resolve();
    expect(store.items()).toEqual([]);
    expect(store.isEmpty()).toBe(true);
  });

  describe('addToCart', () => {
    it('should add a new item', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user',
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
        buyerId: 'test-user', items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      mockCartService.updateItem.mockResolvedValueOnce({
        buyerId: 'test-user', items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 5, price: 10, lineTotal: 50 }]
      });
      await store.updateQuantity(PRODUCT_ID, 5);
      expect(mockCartService.updateItem).toHaveBeenCalledWith(PRODUCT_ID, 5);
    });

    it('should remove if quantity is 0', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      mockCartService.removeItem.mockResolvedValueOnce({ buyerId: 'test-user', items: [] });
      await store.updateQuantity(PRODUCT_ID, 0);
      expect(store.items()).toEqual([]);
    });
  });

  describe('removeFromCart', () => {
    it('should remove item', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);

      mockCartService.removeItem.mockResolvedValueOnce({ buyerId: 'test-user', items: [] });
      await store.removeFromCart(PRODUCT_ID);
      expect(mockCartService.removeItem).toHaveBeenCalledWith(PRODUCT_ID);
    });
  });

  describe('checkout', () => {
    it('should checkout and clear cart', async () => {
      mockCartService.addItem.mockResolvedValueOnce({
        buyerId: 'test-user', items: [{ productId: PRODUCT_ID, storeId: 's1', quantity: 1, price: 10, lineTotal: 10 }]
      });
      await store.addToCart(PRODUCT_ID, 1);
      await store.checkout();
      expect(store.items()).toEqual([]);
      expect(store.checkoutCorrelationId()).toBe('test-correlation-id');
    });
  });
});
