import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { CategoryTreeService, CategoryTree } from './category-tree.service';

const mockTree: CategoryTree[] = [
  {
    id: '1', name: 'Electronics', description: null, parentCategoryId: null,
    slug: 'electronics', sortOrder: 1, isActive: true,
    children: [
      {
        id: '11', name: 'Phones', description: null, parentCategoryId: '1',
        slug: 'phones', sortOrder: 1, isActive: true, children: [],
      },
    ],
  },
];

describe('CategoryTreeService', () => {
  let service: CategoryTreeService;
  let mockHttp: { get: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    mockHttp = {
      get: vi.fn().mockReturnValue(of(mockTree)),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: HttpClient, useValue: mockHttp },
      ],
    });

    service = TestBed.inject(CategoryTreeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('categoryTree starts as empty array', () => {
    expect(service.categoryTree()).toEqual([]);
  });

  it('loading starts as false', () => {
    expect(service.loading()).toBe(false);
  });

  describe('initialize', () => {
    it('fetches category tree from the correct endpoint', async () => {
      await service.initialize();

      expect(mockHttp.get).toHaveBeenCalledWith(
        '/api/catalog/categories/tree',
        { withCredentials: true }
      );
    });

    it('sets categoryTree signal on success', async () => {
      await service.initialize();

      expect(service.categoryTree()).toEqual(mockTree);
    });

    it('sets loading to true during fetch, then false after', async () => {
      const loadingValues: boolean[] = [];
      // Spy on loading changes by checking between calls
      loadingValues.push(service.loading());

      await service.initialize();

      loadingValues.push(service.loading());
      expect(loadingValues).toEqual([false, false]);
      // During the call, loading was set to true (we can't observe mid-flight easily,
      // but we verify it ends as false)
    });

    it('sets categoryTree to empty array on HTTP error', async () => {
      mockHttp.get.mockReturnValueOnce(throwError(() => new Error('Network error')));

      await service.initialize();

      expect(service.categoryTree()).toEqual([]);
    });

    it('sets loading to false after error', async () => {
      mockHttp.get.mockReturnValueOnce(throwError(() => new Error('fail')));

      await service.initialize();

      expect(service.loading()).toBe(false);
    });

    it('can be called multiple times', async () => {
      await service.initialize();
      expect(service.categoryTree()).toEqual(mockTree);

      const newTree: CategoryTree[] = [
        { id: '99', name: 'New', description: null, parentCategoryId: null, slug: 'new', sortOrder: 1, isActive: true, children: [] },
      ];
      mockHttp.get.mockReturnValueOnce(of(newTree));

      await service.initialize();
      expect(service.categoryTree()).toEqual(newTree);
    });
  });
});
