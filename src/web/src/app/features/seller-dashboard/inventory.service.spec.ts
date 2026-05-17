// SellerInventoryService unit tests.
// Tests HTTP calls for inventory operations: getInventoryBySkus and addStock.

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SellerInventoryService } from './inventory.service';

describe('SellerInventoryService', () => {
  let service: SellerInventoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SellerInventoryService],
    });
    service = TestBed.inject(SellerInventoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getInventoryBySkus', () => {
    it('should POST /api/inventory/items/batch with skus', async () => {
      const mockResponse = [
        { id: '1', sku: 'SKU-1', availableQuantity: 10 },
        { id: '2', sku: 'SKU-2', availableQuantity: 3 },
      ];
      const promise = service.getInventoryBySkus(['SKU-1', 'SKU-2']);

      const req = httpMock.expectOne('/api/inventory/items/batch');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ skus: ['SKU-1', 'SKU-2'] });
      req.flush(mockResponse);

      const result = await promise;
      expect(result).toEqual(mockResponse);
    });

    it('should handle empty skus array', async () => {
      const promise = service.getInventoryBySkus([]);

      const req = httpMock.expectOne('/api/inventory/items/batch');
      expect(req.request.body).toEqual({ skus: [] });
      req.flush([]);

      const result = await promise;
      expect(result).toEqual([]);
    });
  });

  describe('addStock', () => {
    it('should POST /api/inventory/items/{sku}/add-stock with quantity', async () => {
      const promise = service.addStock('SKU-1', 5);

      const req = httpMock.expectOne('/api/inventory/items/SKU-1/add-stock');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ quantity: 5 });
      req.flush(null);

      await promise;
    });
  });
});
