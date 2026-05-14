import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthStore } from './auth.store';
import { AuthService } from './auth.service';

describe('AuthStore', () => {
  let authServiceMock: any;
  let routerMock: any;

  beforeEach(() => {
    authServiceMock = {
      login: vi.fn().mockResolvedValue(undefined),
      register: vi.fn().mockResolvedValue(undefined),
      logout: vi.fn().mockResolvedValue(undefined),
      getUser: vi.fn().mockResolvedValue({ id: '1', email: 'test@test.com' }),
      ensureCsrf: vi.fn().mockResolvedValue(undefined),
    };

    routerMock = {
      navigate: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('should have initial state user=null, loading=false, error=null', () => {
    const store = TestBed.inject(AuthStore);
    expect(store.user()).toBeNull();
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('login method updates state on success', async () => {
    const store = TestBed.inject(AuthStore);
    await store.login({ email: 'test@test.com', password: 'password' });

    expect(authServiceMock.login).toHaveBeenCalledWith({ email: 'test@test.com', password: 'password' });
    expect(authServiceMock.ensureCsrf).toHaveBeenCalled();
    expect(authServiceMock.getUser).toHaveBeenCalled();

    expect(store.loading()).toBe(false);
    expect(store.user()).toEqual({ id: '1', email: 'test@test.com' });
    expect(store.error()).toBeNull();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/catalog']);
  });

  it('login method updates state on error', async () => {
    authServiceMock.login.mockRejectedValue({ error: { error: 'Invalid credentials' } });
    const store = TestBed.inject(AuthStore);
    await store.login({ email: 'test@test.com', password: 'wrong' });

    expect(store.loading()).toBe(false);
    expect(store.user()).toBeNull();
    expect(store.error()).toBe('Invalid credentials');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('logout method clears user state', async () => {
    const store = TestBed.inject(AuthStore);
    await store.logout();

    expect(authServiceMock.logout).toHaveBeenCalled();
    expect(store.loading()).toBe(false);
    expect(store.user()).toBeNull();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/auth/login']);
  });
});
