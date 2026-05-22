import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { AuthService } from './auth.service';
import { User, LoginCredentials, RegisterCredentials } from './auth.models';
import { Router } from '@angular/router';
import { NotificationService } from '../signalr/notification.service';
import { CartStore } from '../../features/cart/cart.store';
import { extractHttpError } from '../utils/http.utils';

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
  withMethods((
    store,
    authService = inject(AuthService),
    router = inject(Router),
    notificationService = inject(NotificationService),
    cartStore = inject(CartStore),
    platformId = inject(PLATFORM_ID)) => {

    // ── Shared helpers (DRY login/register post-auth flow) ─────

    async function setupUser(user: User): Promise<void> {
      if (isPlatformBrowser(platformId)) {
        localStorage.setItem('buyerId', user.id);
      }
      await notificationService.start(user.id);
    }

    async function cleanupAuth(): Promise<void> {
      if (isPlatformBrowser(platformId)) {
        localStorage.removeItem('buyerId');
      }
      await notificationService.stop();
      patchState(store, { user: null, loading: false });
      router.navigate(['/auth/login']);
    }

    async function handlePostAuth(): Promise<void> {
      await authService.ensureCsrf();
      const user = await authService.getUser();
      if (user) {
        await setupUser(user);
      }
      patchState(store, { user, loading: false });
      await cartStore.refreshAfterLogin();
      router.navigate(['/catalog']);
    }

    // ── Public API ─────────────────────────────────────────────

    return {
      async login(credentials: LoginCredentials): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          await authService.login(credentials);
          await handlePostAuth();
        } catch (err: unknown) {
          patchState(store, {
            error: extractHttpError(err, 'Invalid credentials'),
            loading: false,
          });
        }
      },

      async register(credentials: RegisterCredentials): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          await authService.register(credentials);
          await handlePostAuth();
        } catch (err: unknown) {
          patchState(store, {
            error: extractHttpError(err, 'Registration failed'),
            loading: false,
          });
        }
      },

      async logout(): Promise<void> {
        patchState(store, { loading: true });
        try {
          await authService.ensureCsrf();
          await authService.logout();
        } catch {
          // Logout should always clean up locally, even if the API call fails
        } finally {
          await cleanupAuth();
        }
      },

      async checkAuth(): Promise<void> {
        patchState(store, { loading: true });
        try {
          await authService.ensureCsrf();
          const user = await authService.getUser();
          if (user) {
            await setupUser(user);
          }
          patchState(store, { user, loading: false });
        } catch {
          await cleanupAuth();
        }
      },
    };
  }),
);
