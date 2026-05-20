import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CartService } from './cart.service';
import { ShoppingCart, CheckoutResponse } from './cart.models';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;

  const mockCart: ShoppingCart = {
    buyerId: 'test-user',
    items: [{ productId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', title: 'Test Product', imageUrl: null, storeId: '33333333-3333-3333-3333-333333333333', quantity: 2, price: 10, lineTotal: 20 }],
    totalPrice: 20,
    totalItems: 2,
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

  describe('getCart', () => {
    it('should GET /api/cart', async () => {
      const promise = service.getCart();
      const req = httpMock.expectOne('/api/cart');
      expect(req.request.method).toBe('GET');
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
});
