// Store settings store.
// NgRx SignalStore managing store settings and sales summary.
// Currently uses stubbed data until Phase 6 (StoreManagement.API) is built.

import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { StoreService } from './store.service';
import { StoreSettings, SalesSummary } from './seller.models';

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
  })),

  withMethods((store, storeService = inject(StoreService)) => ({

    async loadSettings(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const settings = await storeService.getStoreSettings();
        patchState(store, { settings, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load store settings', loading: false });
      }
    },

    async updateSettings(updates: Partial<StoreSettings>): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        const settings = await storeService.updateStoreSettings(updates);
        patchState(store, { settings, loading: false });
        return true;
      } catch {
        patchState(store, { error: 'Failed to update store settings', loading: false });
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
