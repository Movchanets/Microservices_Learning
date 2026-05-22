import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface InventoryItem {
  sku: string;
  availableQuantity: number;
}

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);

  checkStock(sku: string): Promise<InventoryItem> {
    return firstValueFrom(
      this.http.get<InventoryItem>(`/api/inventory/items/${sku}`),
    );
  }
}
