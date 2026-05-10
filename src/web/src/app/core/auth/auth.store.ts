import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { AuthService } from './auth.service';
import { User, LoginCredentials, RegisterCredentials } from './auth.models';
import { Router } from '@angular/router';

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
        patchState(store, { user, loading: false });
        router.navigate(['/']);
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
        patchState(store, { user, loading: false });
        router.navigate(['/']);
      } catch (err: any) {
        patchState(store, { error: err.error?.error || 'Registration failed', loading: false });
      }
    },
    async logout() {
      patchState(store, { loading: true });
      try {
        await authService.ensureCsrf();
        await authService.logout();
        patchState(store, { user: null, loading: false });
        router.navigate(['/login']);
      } catch {
        patchState(store, { user: null, loading: false });
        router.navigate(['/login']);
      }
    },
    async checkAuth() {
      patchState(store, { loading: true });
      try {
        await authService.ensureCsrf();
        const user = await authService.getUser();
        patchState(store, { user, loading: false });
      } catch {
        patchState(store, { user: null, loading: false });
      }
    }
  }))
);
