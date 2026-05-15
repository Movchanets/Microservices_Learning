// Store service (stubbed).
// Provides store settings and sales summary data.
// Currently returns mock data until Phase 6 (StoreManagement.API) is built.

import { Injectable } from '@angular/core';
import { StoreSettings, SalesSummary } from './seller.models';

@Injectable({ providedIn: 'root' })
export class StoreService {
  async getStoreSettings(): Promise<StoreSettings> {
    // Stubbed until Phase 6 (StoreManagement.API) is built
    return {
      storeId: 'store-1',
      storeName: 'My Store',
      description: 'A sample store',
      logoUrl: null,
      contactEmail: 'seller@example.com',
      isActive: true,
    };
  }

  async updateStoreSettings(settings: Partial<StoreSettings>): Promise<StoreSettings> {
    // Stubbed — will call PUT /api/stores/{storeId} when Phase 6 is ready
    return this.getStoreSettings();
  }

  async getSalesSummary(): Promise<SalesSummary> {
    // Stubbed — will call GET /api/stores/{storeId}/sales when Phase 6 is ready
    return {
      totalOrders: 0,
      totalRevenue: 0,
      pendingOrders: 0,
      completedOrders: 0,
    };
  }
}
