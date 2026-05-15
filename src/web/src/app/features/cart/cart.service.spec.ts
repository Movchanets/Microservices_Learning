import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CartService } from './cart.service';
import { ShoppingCart, CheckoutResponse } from './cart.models';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;

  const mockCart: ShoppingCart = {
    buyerId: 'test-user',
    items: [{ sku: 'PROD-1', quantity: 2 }]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CartService]
    });
    service = TestBed.inject(CartService);
    httpMock = TestBed.inject(HttpTestingController);

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getCart', () => {
    it('should issue a GET request to /api/cart', async () => {
      const promise = service.getCart();

      const req = httpMock.expectOne('/api/cart');
      expect(req.request.method).toBe('GET');
      req.flush(mockCart);

      const result = await promise;
      expect(result).toEqual(mockCart);
    });

    it('should set x-buyer-id header', async () => {
      localStorage.setItem('buyerId', 'test-buyer-123');
      const promise = service.getCart();

      const req = httpMock.expectOne('/api/cart');
      expect(req.request.headers.get('x-buyer-id')).toBe('test-buyer-123');
      req.flush(mockCart);

      await promise;
    });
  });

  // The assignment mentions addItem, updateQuantity, removeItem,
  // but cart.service.ts only has updateCart. Testing updateCart.
  describe('updateCart', () => {
    it('should issue a POST request to /api/cart', async () => {
      const promise = service.updateCart(mockCart);

      const req = httpMock.expectOne('/api/cart');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(mockCart);
      req.flush(mockCart);

      const result = await promise;
      expect(result).toEqual(mockCart);
    });
  });

  describe('deleteCart', () => {
    it('should issue a DELETE request to /api/cart', async () => {
      const promise = service.deleteCart();

      const req = httpMock.expectOne('/api/cart');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      await promise;
    });
  });

  describe('checkout', () => {
    it('should issue a POST request to /api/cart/checkout', async () => {
      const mockResponse: CheckoutResponse = { correlationId: 'corr-123' };
      const promise = service.checkout();

      const req = httpMock.expectOne('/api/cart/checkout');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockResponse);

      const result = await promise;
      expect(result).toEqual(mockResponse);
    });
  });
});
