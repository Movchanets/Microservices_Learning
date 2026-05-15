// SellerProductService unit tests.
// Verifies HTTP calls to the Catalog API: GET /api/catalog/products?sellerId,
// GET /api/catalog/products/{id}, POST, PUT, DELETE.
// Uses HttpClientTestingModule to assert correct URLs and methods.

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SellerProductService } from './seller-product.service';

describe('SellerProductService', () => {
  let service: SellerProductService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SellerProductService],
    });
    service = TestBed.inject(SellerProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getMyProducts', () => {
    it('should GET /api/catalog/products?sellerId={sellerId}', async () => {
      const mockProducts = [{ id: 'prod-1', name: 'Widget' }];
      const promise = service.getMyProducts('seller-1');

      const req = httpMock.expectOne('/api/catalog/products?sellerId=seller-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockProducts);

      const result = await promise;
      expect(result).toEqual(mockProducts);
    });
  });

  describe('getProductById', () => {
    it('should GET /api/catalog/products/{id}', async () => {
      const mockProduct = { id: 'prod-1', name: 'Widget' };
      const promise = service.getProductById('prod-1');

      const req = httpMock.expectOne('/api/catalog/products/prod-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockProduct);

      const result = await promise;
      expect(result).toEqual(mockProduct);
    });
  });

  describe('createProduct', () => {
    it('should POST /api/catalog/products', async () => {
      const request = { name: 'New Widget', sku: 'W-1', description: 'A new widget', price: 10, currency: 'USD', categoryId: 'cat-1', sellerId: 'seller-1' };
      const mockResponse = { id: 'prod-2', ...request };
      const promise = service.createProduct(request);

      const req = httpMock.expectOne('/api/catalog/products');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);

      const result = await promise;
      expect(result).toEqual(mockResponse);
    });
  });

  describe('updateProduct', () => {
    it('should PUT /api/catalog/products/{id}', async () => {
      const request = { name: 'Updated Widget' };
      const mockResponse = { id: 'prod-1', name: 'Updated Widget' };
      const promise = service.updateProduct('prod-1', request);

      const req = httpMock.expectOne('/api/catalog/products/prod-1');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);

      const result = await promise;
      expect(result).toEqual(mockResponse);
    });
  });

  describe('deleteProduct', () => {
    it('should DELETE /api/catalog/products/{id}', async () => {
      const promise = service.deleteProduct('prod-1');

      const req = httpMock.expectOne('/api/catalog/products/prod-1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      await promise;
    });
  });
});
