// Store service.
// Calls StoreManagement.API for store settings and sales summary.

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { StoreSettings, SalesSummary } from './seller.models';

interface StoreApiResponse {
  id: string;
  name: string;
  description: string;
  logoUrl: string | null;
  verificationStatus: 'Pending' | 'Verified' | 'Rejected';
  rejectionReason: string | null;
  createdAt: string;
  updatedAt: string | null;
  verifiedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class StoreService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/stores';

  async getStoreBySellerId(sellerId: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.get<StoreApiResponse>(`${this.baseUrl}/seller/${sellerId}`)
    );
    return this.mapToSettings(store);
  }

  async getStoreById(storeId: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.get<StoreApiResponse>(`${this.baseUrl}/${storeId}`)
    );
    return this.mapToSettings(store);
  }

  async createStore(name: string, description: string, sellerId: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.post<StoreApiResponse>(this.baseUrl, { sellerId, name, description })
    );
    return this.mapToSettings(store);
  }

  async updateStore(storeId: string, name: string, description: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.put<StoreApiResponse>(`${this.baseUrl}/${storeId}`, { name, description })
    );
    return this.mapToSettings(store);
  }

  async setLogo(storeId: string, logoUrl: string): Promise<void> {
    await firstValueFrom(
      this.http.put<void>(`${this.baseUrl}/${storeId}/logo`, { logoUrl })
    );
  }

  async getSalesSummary(): Promise<SalesSummary> {
    // TODO: Implement when Ordering.API has a sales summary endpoint
    return {
      totalOrders: 0,
      totalRevenue: 0,
      pendingOrders: 0,
      completedOrders: 0,
    };
  }

  private mapToSettings(store: StoreApiResponse): StoreSettings {
    return {
      storeId: store.id,
      storeName: store.name,
      description: store.description,
      logoUrl: store.logoUrl,
      contactEmail: '',
      isActive: store.verificationStatus === 'Verified',
      verificationStatus: store.verificationStatus,
      rejectionReason: store.rejectionReason,
      createdAt: store.createdAt,
      verifiedAt: store.verifiedAt,
    };
  }
}
