import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CatalogService } from './catalog.service';

describe('CatalogService', () => {
  let service: CatalogService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CatalogService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getProducts', () => {
    it('correctly maps ProductListParams to HttpParams', async () => {
      const promise = service.getProducts({ page: 2, pageSize: 50, categoryId: 'cat123', search: 'test' });

      const req = httpMock.expectOne((request) => {
        return request.url === '/api/catalog/products' &&
               request.params.get('page') === '2' &&
               request.params.get('pageSize') === '50' &&
               request.params.get('categoryId') === 'cat123' &&
               request.params.get('search') === 'test';
      });

      expect(req.request.method).toBe('GET');
      req.flush({ items: [], totalCount: 0 });
      await promise;
    });
  });

  describe('searchProducts', () => {
    it('hits the /api/search/products gateway endpoint', async () => {
      const promise = service.searchProducts({ q: 'phone', page: 1, pageSize: 20 });

      const req = httpMock.expectOne((request) => {
        return request.url === '/api/search/products' &&
               request.params.get('q') === 'phone' &&
               request.params.get('page') === '1' &&
               request.params.get('pageSize') === '20';
      });

      expect(req.request.method).toBe('GET');
      req.flush({ items: [], totalCount: 0, facets: {} });
      await promise;
    });
  });
});
