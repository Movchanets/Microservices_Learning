import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ShoppingCart, CheckoutResponse } from './cart.models';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/cart';

  async getCart(): Promise<ShoppingCart> {
    return firstValueFrom(this.http.get<ShoppingCart>(this.baseUrl));
  }

  async updateCart(cart: ShoppingCart): Promise<ShoppingCart> {
    return firstValueFrom(this.http.post<ShoppingCart>(this.baseUrl, cart));
  }

  async deleteCart(): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.baseUrl));
  }

  async checkout(address?: any): Promise<CheckoutResponse> {
    return firstValueFrom(this.http.post<CheckoutResponse>(`${this.baseUrl}/checkout`, address || {}));
  }

  async addItem(sku: string, quantity: number, sellerId?: string): Promise<ShoppingCart> {
    return firstValueFrom(this.http.post<ShoppingCart>(`${this.baseUrl}/items`, { sku, quantity, sellerId }));
  }

  async updateItem(sku: string, quantity: number): Promise<ShoppingCart> {
    return firstValueFrom(this.http.put<ShoppingCart>(`${this.baseUrl}/items/${sku}`, { quantity }));
  }

  async removeItem(sku: string): Promise<ShoppingCart> {
    return firstValueFrom(this.http.delete<ShoppingCart>(`${this.baseUrl}/items/${sku}`));
  }
}
