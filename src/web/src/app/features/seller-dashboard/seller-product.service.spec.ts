// SellerProductService unit tests.
// Verifies HTTP calls to the Catalog API: GET /api/catalog/products?storeId,
// GET /api/catalog/products/{id}, POST, PUT, DELETE, addSku, removeSku, changeSkuPrice.
// Uses HttpClientTestingModule to assert correct URLs and methods.

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SellerProductService } from './seller-product.service';

describe('SellerProductService', () => {
  let service: SellerProductService;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [SellerProductService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SellerProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getMyProducts', () => {
    it('should GET /api/catalog/products?storeId={storeId}', async () => {
      const mockProducts = [{ id: 'prod-1', name: 'Widget' }];
      const mockPaged = { items: mockProducts, totalCount: 1, page: 1, pageSize: 20, totalPages: 1, hasPrevious: false, hasNext: false };
      const promise = service.getMyProducts('store-1');

      const req = httpMock.expectOne('/api/catalog/products?storeId=store-1&status=All');
      expect(req.request.method).toBe('GET');
      req.flush(mockPaged);

      const result = await promise;
      expect(result).toEqual(mockProducts);
    });
  });

  describe('getProductById', () => {
    it('should GET /api/catalog/products/{id}', async () => {
      const mockProduct = { id: 'prod-1', name: 'Widget', skus: [] };
      const promise = service.getProductById('prod-1');

      const req = httpMock.expectOne('/api/catalog/products/prod-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockProduct);

      const result = await promise;
      expect(result).toEqual(mockProduct);
    });
  });

  describe('createProduct', () => {
    it('should POST /api/catalog/products with product metadata only', async () => {
      const request = { name: 'New Widget', description: 'A new widget', categoryId: 'cat-1', storeId: 'store-1', brand: 'TestBrand' };
      const mockResponse = { id: 'prod-2', ...request, skus: [] };
      const promise = service.createProduct(request);

      const req = httpMock.expectOne('/api/catalog/products');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);

      const result = await promise;
      expect(result).toEqual(mockResponse);
    });
  });

  describe('addSku', () => {
    it('should POST /api/catalog/products/{id}/skus', async () => {
      const request = { skuCode: 'W-01', price: 29.99, currency: 'USD', typedAttributes: { color: 'Red' } };
      const mockResponse = { id: 'sku-1', ...request, status: 'Active', typedAttributes: { color: 'Red' }, flexibleAttributes: {}, createdAt: '2026-01-01' };
      const promise = service.addSku('prod-1', request);

      const req = httpMock.expectOne('/api/catalog/products/prod-1/skus');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);

      const result = await promise;
      expect(result).toEqual(mockResponse);
    });
  });

  describe('removeSku', () => {
    it('should DELETE /api/catalog/products/{productId}/skus/{skuId}', async () => {
      const promise = service.removeSku('prod-1', 'sku-1');

      const req = httpMock.expectOne('/api/catalog/products/prod-1/skus/sku-1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      await promise;
    });
  });

  describe('changeSkuPrice', () => {
    it('should PATCH /api/catalog/products/{productId}/skus/{skuId}/price', async () => {
      const promise = service.changeSkuPrice('prod-1', 'sku-1', 49.99, 'EUR');

      const req = httpMock.expectOne('/api/catalog/products/prod-1/skus/sku-1/price');
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual({ price: 49.99, currency: 'EUR' });
      req.flush(null);

      await promise;
    });
  });

  describe('updateProduct', () => {
    it('should PUT /api/catalog/products/{id}', async () => {
      const request = { name: 'Updated Widget', description: 'A widget', categoryId: 'cat-1' };
      const mockResponse = { id: 'prod-1', name: 'Updated Widget', skus: [] };
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

  describe('activateProduct', () => {
    it('should PUT /api/catalog/products/{id}/activate', async () => {
      const promise = service.activateProduct('prod-1');

      const req = httpMock.expectOne('/api/catalog/products/prod-1/activate');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({});
      req.flush(null);

      await promise;
    });
  });

  describe('deactivateProduct', () => {
    it('should PUT /api/catalog/products/{id}/deactivate', async () => {
      const promise = service.deactivateProduct('prod-1');

      const req = httpMock.expectOne('/api/catalog/products/prod-1/deactivate');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({});
      req.flush(null);

      await promise;
    });
  });
});
