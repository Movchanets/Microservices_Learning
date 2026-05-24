import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CartService } from './cart.service';
import { ShoppingCart, CheckoutResponse } from './cart.models';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;

  const mockCart: ShoppingCart = {
    buyerId: 'test-user',
    cartId: 'cart-123',
    items: [{ productId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', title: 'Test Product', imageUrl: null, storeId: '33333333-3333-3333-3333-333333333333', quantity: 2, price: 10, lineTotal: 20 }],
    totalPrice: 20,
    totalItems: 2,
  };

  const mockAnonCart: ShoppingCart = {
    buyerId: null,
    cartId: 'anon-cart-456',
    items: [{ productId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', title: 'Test Product', imageUrl: null, storeId: '33333333-3333-3333-3333-333333333333', quantity: 1, price: 15, lineTotal: 15 }],
    totalPrice: 15,
    totalItems: 1,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CartService]
    });
    service = TestBed.inject(CartService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => { httpMock.verify(); });

  it('should be created', () => { expect(service).toBeTruthy(); });

  // ── CartId localStorage management ──

  describe('cartId management', () => {
    it('should return null when no cartId stored', () => {
      expect(service.getCartId()).toBeNull();
    });

    it('should store and retrieve cartId', () => {
      service.setCartId('test-cart-id');
      expect(service.getCartId()).toBe('test-cart-id');
    });

    it('should clear cartId', () => {
      service.setCartId('test-cart-id');
      service.clearCartId();
      expect(service.getCartId()).toBeNull();
    });

    it('should overwrite existing cartId', () => {
      service.setCartId('old-id');
      service.setCartId('new-id');
      expect(service.getCartId()).toBe('new-id');
    });
  });

  // ── Authenticated user flow ──

  describe('getCart (authenticated)', () => {
    it('should GET /bff/cart without X-Cart-Id header', async () => {
      const promise = service.getCart();
      const req = httpMock.expectOne('/bff/cart');
      expect(req.request.method).toBe('GET');
      expect(req.request.headers.has('X-Cart-Id')).toBe(false);
      req.flush(mockCart);
      expect(await promise).toEqual(mockCart);
    });
  });

  describe('addItem', () => {
    it('should POST /api/cart/items with productId and quantity', async () => {
      const productId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
      const promise = service.addItem(productId, 3);
      const req = httpMock.expectOne('/api/cart/items');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ productId, quantity: 3 });
      req.flush(mockCart);
      expect(await promise).toEqual(mockCart);
    });
  });

  describe('updateItem', () => {
    it('should PUT /api/cart/items/{productId}', async () => {
      const productId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
      const promise = service.updateItem(productId, 5);
      const req = httpMock.expectOne(`/api/cart/items/${productId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ quantity: 5 });
      req.flush(mockCart);
      expect(await promise).toEqual(mockCart);
    });
  });

  describe('removeItem', () => {
    it('should DELETE /api/cart/items/{productId}', async () => {
      const productId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
      const promise = service.removeItem(productId);
      const req = httpMock.expectOne(`/api/cart/items/${productId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ ...mockCart, items: [] });
      const result = await promise;
      expect(result.items).toEqual([]);
    });
  });

  describe('deleteCart', () => {
    it('should DELETE /api/cart', async () => {
      const promise = service.deleteCart();
      const req = httpMock.expectOne('/api/cart');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
      await promise;
    });
  });

  describe('checkout', () => {
    it('should POST /api/cart/checkout', async () => {
      const mockResponse: CheckoutResponse = { correlationId: 'corr-123' };
      const promise = service.checkout();
      const req = httpMock.expectOne('/api/cart/checkout');
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
      expect(await promise).toEqual(mockResponse);
    });
  });

  // ── Anonymous user flow ──

  describe('anonymous cart flow', () => {
    it('should send X-Cart-Id header when cartId is stored', async () => {
      service.setCartId('anon-cart-456');

      const promise = service.getCart();
      const req = httpMock.expectOne('/bff/cart');
      expect(req.request.headers.get('X-Cart-Id')).toBe('anon-cart-456');
      req.flush(mockAnonCart);
      expect(await promise).toEqual(mockAnonCart);
    });

    it('should not send X-Cart-Id header when no cartId stored', async () => {
      const promise = service.getCart();
      const req = httpMock.expectOne('/bff/cart');
      expect(req.request.headers.has('X-Cart-Id')).toBe(false);
      req.flush(mockAnonCart);
      await promise;
    });

    it('should send X-Cart-Id on addItem for anonymous user', async () => {
      service.setCartId('anon-cart-456');

      const promise = service.addItem('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1);
      const req = httpMock.expectOne('/api/cart/items');
      expect(req.request.headers.get('X-Cart-Id')).toBe('anon-cart-456');
      req.flush(mockAnonCart);
      await promise;
    });

    it('should send X-Cart-Id on updateItem for anonymous user', async () => {
      service.setCartId('anon-cart-456');

      const promise = service.updateItem('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 3);
      const req = httpMock.expectOne('/api/cart/items/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
      expect(req.request.headers.get('X-Cart-Id')).toBe('anon-cart-456');
      req.flush(mockAnonCart);
      await promise;
    });

    it('should send X-Cart-Id on removeItem for anonymous user', async () => {
      service.setCartId('anon-cart-456');

      const promise = service.removeItem('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
      const req = httpMock.expectOne('/api/cart/items/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
      expect(req.request.headers.get('X-Cart-Id')).toBe('anon-cart-456');
      req.flush({ ...mockAnonCart, items: [] });
      await promise;
    });

    it('should send X-Cart-Id on deleteCart for anonymous user', async () => {
      service.setCartId('anon-cart-456');

      const promise = service.deleteCart();
      const req = httpMock.expectOne('/api/cart');
      expect(req.request.headers.get('X-Cart-Id')).toBe('anon-cart-456');
      req.flush(null);
      await promise;
    });

    it('should send X-Cart-Id on checkout for anonymous cart merge', async () => {
      service.setCartId('anon-cart-456');

      const promise = service.checkout();
      const req = httpMock.expectOne('/api/cart/checkout');
      // Checkout now includes X-Cart-Id to support anon→auth merge
      expect(req.request.headers.get('X-Cart-Id')).toBe('anon-cart-456');
      req.flush({ correlationId: 'corr-789' });
      await promise;
    });

    it('should handle anonymous cart response with null buyerId', async () => {
      const promise = service.getCart();
      const req = httpMock.expectOne('/bff/cart');
      req.flush(mockAnonCart);
      const result = await promise;
      expect(result.buyerId).toBeNull();
      expect(result.cartId).toBe('anon-cart-456');
      expect(result.items.length).toBe(1);
    });
  });
});
