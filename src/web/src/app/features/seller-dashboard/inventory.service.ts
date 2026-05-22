import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface InventoryItemResponse {
  id: string;
  sku: string;
  availableQuantity: number;
}

@Injectable({ providedIn: 'root' })
export class SellerInventoryService {
  private readonly http = inject(HttpClient);

  getInventoryBySkus(skus: string[]): Promise<InventoryItemResponse[]> {
    return firstValueFrom(
      this.http.post<InventoryItemResponse[]>('/api/inventory/items/batch', { skus }),
    );
  }

  addStock(sku: string, quantity: number): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`/api/inventory/items/${sku}/add-stock`, { quantity }),
    );
  }
}
