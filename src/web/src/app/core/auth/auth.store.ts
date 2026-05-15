import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { AuthService } from './auth.service';
import { User, LoginCredentials, RegisterCredentials } from './auth.models';
import { Router } from '@angular/router';
import { NotificationService } from '../signalr/notification.service';

type AuthState = {
  user: User | null;
  loading: boolean;
  error: string | null;
};

const initialState: AuthState = {
  user: null,
  loading: false,
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, authService = inject(AuthService), router = inject(Router)) => ({
    async login(credentials: LoginCredentials) {
      patchState(store, { loading: true, error: null });
      try {
        await authService.login(credentials);
        await authService.ensureCsrf();
        const user = await authService.getUser();
        if (user) localStorage.setItem('buyerId', user.id);
        patchState(store, { user, loading: false });
        router.navigate(['/catalog']);
      } catch (err: any) {
        patchState(store, { error: err.error?.error || 'Invalid credentials', loading: false });
      }
    },
    async register(credentials: RegisterCredentials) {
      patchState(store, { loading: true, error: null });
      try {
        await authService.register(credentials);
        await authService.ensureCsrf();
        const user = await authService.getUser();
        if (user) localStorage.setItem('buyerId', user.id);
        patchState(store, { user, loading: false });
        router.navigate(['/catalog']);
      } catch (err: any) {
        patchState(store, { error: err.error?.error || 'Registration failed', loading: false });
      }
    },
    async logout() {
      patchState(store, { loading: true });
      try {
        inject(NotificationService).stop();
        localStorage.removeItem('buyerId');
        await authService.ensureCsrf();
        await authService.logout();
        patchState(store, { user: null, loading: false });
        router.navigate(['/auth/login']);
      } catch {
        localStorage.removeItem('buyerId');
        patchState(store, { user: null, loading: false });
        router.navigate(['/auth/login']);
      }
    },
    async checkAuth() {
      patchState(store, { loading: true });
      try {
        await authService.ensureCsrf();
        const user = await authService.getUser();
        if (user) localStorage.setItem('buyerId', user.id);
        patchState(store, { user, loading: false });
      } catch {
        localStorage.removeItem('buyerId');
        patchState(store, { user: null, loading: false });
      }
    },
  })),
);
