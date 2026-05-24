// Store settings store.
// NgRx SignalStore managing store settings and sales summary.
// Uses StoreManagement.API via StoreService.

import { computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { StoreService } from './store.service';
import { StoreSettings, SalesSummary } from './seller.models';
import { AuthStore } from '../../core/auth/auth.store';
import { extractHttpError } from '../../core/utils/http.utils';

interface StoreSettingsState {
  settings: StoreSettings | null;
  salesSummary: SalesSummary | null;
  loading: boolean;
  error: string | null;
}

const initialState: StoreSettingsState = {
  settings: null,
  salesSummary: null,
  loading: false,
  error: null,
};

export const StoreSettingsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed((store) => ({
    hasSettings: computed(() => store.settings() !== null),
    storeId: computed(() => store.settings()?.storeId || null),
  })),

  withMethods((store, storeService = inject(StoreService), authStore = inject(AuthStore), platformId = inject(PLATFORM_ID)) => ({

    async loadSettings(): Promise<void> {
      // Skip if already loaded or currently loading
      if (store.settings() !== null || store.loading()) return;

      patchState(store, { loading: true, error: null });
      try {
        const user = authStore.user();
        if (!user) {
          patchState(store, { error: 'Not authenticated', loading: false });
          return;
        }
        const settings = await storeService.getStoreBySellerId(user.id);
        patchState(store, { settings, loading: false });
        if (isPlatformBrowser(platformId) && settings.storeId) {
          localStorage.setItem('storeId', settings.storeId);
        }
      } catch (err: unknown) {
        // 404 means seller has no store yet — not an error
        if (err instanceof HttpErrorResponse && err.status === 404) {
          patchState(store, { settings: null, loading: false });
        } else {
          patchState(store, { error: 'Failed to load store settings', loading: false });
        }
      }
    },

    async createStore(name: string, description: string): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        const user = authStore.user();
        if (!user) {
          patchState(store, { error: 'Not authenticated', loading: false });
          return false;
        }
        const settings = await storeService.createStore(name, description, user.id);
        patchState(store, { settings, loading: false });
        if (isPlatformBrowser(platformId) && settings.storeId) {
          localStorage.setItem('storeId', settings.storeId);
        }
        return true;
      } catch (err: unknown) {
        patchState(store, {
          error: extractHttpError(err, 'Failed to create store'),
          loading: false,
        });
        return false;
      }
    },

    async updateSettings(name: string, description: string): Promise<boolean> {
      const currentSettings = store.settings();
      if (!currentSettings?.storeId) {
        patchState(store, { error: 'No store found', loading: false });
        return false;
      }
      patchState(store, { loading: true, error: null });
      try {
        const settings = await storeService.updateStore(currentSettings.storeId, name, description);
        patchState(store, { settings, loading: false });
        return true;
      } catch (err: unknown) {
        patchState(store, {
          error: extractHttpError(err, 'Failed to update store settings'),
          loading: false,
        });
        return false;
      }
    },

    async setLogo(logoUrl: string): Promise<boolean> {
      const currentSettings = store.settings();
      if (!currentSettings?.storeId) {
        patchState(store, { error: 'No store found', loading: false });
        return false;
      }
      patchState(store, { loading: true, error: null });
      try {
        await storeService.setLogo(currentSettings.storeId, logoUrl);
        patchState(store, {
          settings: { ...currentSettings, logoUrl },
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to update logo', loading: false });
        return false;
      }
    },

    async loadSalesSummary(): Promise<void> {
      try {
        const salesSummary = await storeService.getSalesSummary();
        patchState(store, { salesSummary });
      } catch {
        // Non-critical — don't set error state
      }
    },
  }))
);
