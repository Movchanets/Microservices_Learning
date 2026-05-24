import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { LoginCredentials, RegisterCredentials, User } from './auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(AuthService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('login should call /bff/auth/login with POST', async () => {
    const credentials: LoginCredentials = { email: 'test@test.com', password: 'password' };

    const promise = service.login(credentials);
    const req = httpTestingController.expectOne('/bff/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(credentials);

    req.flush(null);
    await expect(promise).resolves.toBeNull();
  });

  it('register should call /bff/auth/register with POST', async () => {
    const credentials: RegisterCredentials = { firstName: 'John', lastName: 'Doe', email: 'test@test.com', password: 'password' };

    const promise = service.register(credentials);
    const req = httpTestingController.expectOne('/bff/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(credentials);

    req.flush(null);
    await expect(promise).resolves.toBeNull();
  });

  it('logout should call /bff/auth/logout with POST', async () => {
    const promise = service.logout();
    const req = httpTestingController.expectOne('/bff/auth/logout');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});

    req.flush(null);
    await expect(promise).resolves.toBeNull();
  });

  it('getUser should retrieve current user via /bff/user', async () => {
    const mockUser: User = { id: '1', email: 'test@test.com', firstName: 'John', lastName: 'Doe', role: 'Buyer' };

    const promise = service.getUser();
    const req = httpTestingController.expectOne('/bff/user');
    expect(req.request.method).toBe('GET');

    req.flush(mockUser);
    const user = await promise;
    expect(user).toEqual(mockUser);
  });

  it('ensureCsrf should call /bff/csrf with GET', async () => {
    const promise = service.ensureCsrf();
    const req = httpTestingController.expectOne('/bff/csrf');
    expect(req.request.method).toBe('GET');

    req.flush(null);
    await expect(promise).resolves.toBeNull();
  });
});
