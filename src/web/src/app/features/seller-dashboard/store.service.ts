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
}

@Injectable({ providedIn: 'root' })
export class StoreService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/stores';

  async getStoreBySellerId(sellerId: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.get<StoreApiResponse>(`${this.baseUrl}/seller/${sellerId}`)
    );
    return {
      storeId: store.id,
      storeName: store.name,
      description: store.description,
      logoUrl: store.logoUrl,
      contactEmail: '',
      isActive: store.verificationStatus === 'Verified',
      verificationStatus: store.verificationStatus,
    };
  }

  async getStoreById(storeId: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.get<StoreApiResponse>(`${this.baseUrl}/${storeId}`)
    );
    return {
      storeId: store.id,
      storeName: store.name,
      description: store.description,
      logoUrl: store.logoUrl,
      contactEmail: '',
      isActive: store.verificationStatus === 'Verified',
      verificationStatus: store.verificationStatus,
    };
  }

  async createStore(name: string, description: string, sellerId: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.post<StoreApiResponse>(this.baseUrl, { sellerId, name, description })
    );
    return {
      storeId: store.id,
      storeName: store.name,
      description: store.description,
      logoUrl: store.logoUrl,
      contactEmail: '',
      isActive: false, // New stores start as Pending
      verificationStatus: store.verificationStatus,
    };
  }

  async updateStore(storeId: string, name: string, description: string): Promise<StoreSettings> {
    const store = await firstValueFrom(
      this.http.put<StoreApiResponse>(`${this.baseUrl}/${storeId}`, { name, description })
    );
    return {
      storeId: store.id,
      storeName: store.name,
      description: store.description,
      logoUrl: store.logoUrl,
      contactEmail: '',
      isActive: store.verificationStatus === 'Verified',
      verificationStatus: store.verificationStatus,
    };
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
}
