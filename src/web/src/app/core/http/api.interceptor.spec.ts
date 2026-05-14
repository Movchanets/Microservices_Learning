import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { apiInterceptor } from './api.interceptor';

describe('apiInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should automatically add withCredentials: true to all requests', () => {
    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.withCredentials).toBe(true);
  });

  it('should prepend /api Base URL if applicable', () => {
    httpClient.get('/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.url).toBe('/api/test');
  });

  it('should not prepend /api if already absolute or already starts with /api', () => {
    httpClient.get('https://example.com/data').subscribe();
    const req1 = httpMock.expectOne('https://example.com/data');
    expect(req1.request.url).toBe('https://example.com/data');

    httpClient.get('/api/data').subscribe();
    const req2 = httpMock.expectOne('/api/data');
    expect(req2.request.url).toBe('/api/data');
  });
});
