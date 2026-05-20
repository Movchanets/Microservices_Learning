// Store settings store.
// NgRx SignalStore managing store settings and sales summary.
// Uses StoreManagement.API via StoreService.

import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { StoreService } from './store.service';
import { StoreSettings, SalesSummary } from './seller.models';
import { AuthStore } from '../../core/auth/auth.store';

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

  withMethods((store, storeService = inject(StoreService), authStore = inject(AuthStore)) => ({

    async loadSettings(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const user = authStore.user();
        if (!user) {
          patchState(store, { error: 'Not authenticated', loading: false });
          return;
        }
        const settings = await storeService.getStoreBySellerId(user.id);
        patchState(store, { settings, loading: false });
      } catch (err: unknown) {
        const e = err as { status?: number; error?: { error?: string } };
        // If 404, seller has no store yet — not an error
        if (e?.status === 404) {
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
        return true;
      } catch (err: unknown) {
        const e = err as { error?: { error?: string } };
        patchState(store, { error: e?.error?.error || 'Failed to create store', loading: false });
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
        const e = err as { error?: { error?: string } };
        patchState(store, { error: e?.error?.error || 'Failed to update store settings', loading: false });
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
