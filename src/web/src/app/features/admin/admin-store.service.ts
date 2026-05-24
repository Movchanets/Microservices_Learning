// Admin store service.
// Handles store verification operations via StoreManagement.API.
// Uses the BFF pattern - all calls go through /api/stores.

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AdminStore, VerifyStoreRequest } from './admin.models';

@Injectable({ providedIn: 'root' })
export class AdminStoreService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/stores';

  async getAllStores(status?: string): Promise<AdminStore[]> {
    const url = status ? `${this.baseUrl}?status=${status}` : this.baseUrl;
    return firstValueFrom(
      this.http.get<AdminStore[]>(url)
    );
  }

  async getPendingStores(): Promise<AdminStore[]> {
    return this.getAllStores('Pending');
  }

  async getStoreById(id: string): Promise<AdminStore> {
    return firstValueFrom(
      this.http.get<AdminStore>(`${this.baseUrl}/${id}`)
    );
  }

  async verifyStore(storeId: string, request: VerifyStoreRequest): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/${storeId}/verify`, request)
    );
  }
}
