// Admin store.
// NgRx SignalStore managing admin panel state — users, stores, pending verifications.
// Provides computed signals for stats and filtered lists.

import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { AdminUserService } from './admin-user.service';
import { AdminStoreService } from './admin-store.service';
import { AdminUser, AdminStore as AdminStoreModel, VerifyStoreRequest } from './admin.models';

interface AdminState {
  users: AdminUser[];
  stores: AdminStoreModel[];
  pendingStores: AdminStoreModel[];
  selectedStore: AdminStoreModel | null;
  loading: boolean;
  error: string | null;
}

const initialState: AdminState = {
  users: [],
  stores: [],
  pendingStores: [],
  selectedStore: null,
  loading: false,
  error: null,
};

export const AdminStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed((store) => ({
    pendingCount: computed(() => store.pendingStores().length),
    verifiedStores: computed(() => store.stores().filter(s => s.verificationStatus === 'Verified')),
    rejectedStores: computed(() => store.stores().filter(s => s.verificationStatus === 'Rejected')),
    adminUsers: computed(() => store.users().filter(u => u.role === 'Admin')),
    sellerUsers: computed(() => store.users().filter(u => u.role === 'Seller')),
    buyerUsers: computed(() => store.users().filter(u => u.role === 'Buyer')),
    hasUsers: computed(() => store.users().length > 0),
    hasPendingStores: computed(() => store.pendingStores().length > 0),
  })),

  withMethods((
    store,
    userService = inject(AdminUserService),
    storeService = inject(AdminStoreService),
  ) => ({

    async loadUsers(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const users = await userService.getAllUsers();
        patchState(store, { users, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load users', loading: false });
      }
    },

    async loadStores(status?: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const stores = await storeService.getAllStores(status);
        patchState(store, { stores, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load stores', loading: false });
      }
    },

    async loadPendingStores(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const pendingStores = await storeService.getPendingStores();
        patchState(store, { pendingStores, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load pending stores', loading: false });
      }
    },

    async loadStoreById(id: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const selectedStore = await storeService.getStoreById(id);
        patchState(store, { selectedStore, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load store', loading: false });
      }
    },

    async verifyStore(storeId: string, request: VerifyStoreRequest): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        await storeService.verifyStore(storeId, request);
        patchState(store, {
          pendingStores: store.pendingStores().filter(s => s.id !== storeId),
          stores: store.stores().map(s =>
            s.id === storeId
              ? { ...s, verificationStatus: request.isApproved ? 'Verified' as const : 'Rejected' as const }
              : s
          ),
          selectedStore: store.selectedStore()?.id === storeId
            ? { ...store.selectedStore()!, verificationStatus: request.isApproved ? 'Verified' : 'Rejected' }
            : store.selectedStore(),
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to verify store', loading: false });
        return false;
      }
    },

    async updateUserRole(userId: string, role: 'Buyer' | 'Seller' | 'Admin'): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        const updated = await userService.updateUserRole(userId, { role });
        patchState(store, {
          users: store.users().map(u => u.id === userId ? updated : u),
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to update user role', loading: false });
        return false;
      }
    },

    async deactivateUser(userId: string): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        await userService.deactivateUser(userId);
        patchState(store, {
          users: store.users().filter(u => u.id !== userId),
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to deactivate user', loading: false });
        return false;
      }
    },

    clearSelected(): void {
      patchState(store, { selectedStore: null });
    },

    clearError(): void {
      patchState(store, { error: null });
    },
  }))
);
