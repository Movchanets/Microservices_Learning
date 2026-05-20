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
  private readonly bffCartUrl = '/bff/cart';

  async getCart(): Promise<ShoppingCart> {
    return firstValueFrom(this.http.get<ShoppingCart>(this.bffCartUrl));
  }

  async deleteCart(): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.baseUrl));
  }

  async checkout(address?: { addressLine1: string; city: string; state: string; postalCode: string; country: string }): Promise<CheckoutResponse> {
    return firstValueFrom(this.http.post<CheckoutResponse>(`${this.baseUrl}/checkout`, address || {}));
  }

  async addItem(productId: string, quantity: number): Promise<ShoppingCart> {
    return firstValueFrom(this.http.post<ShoppingCart>(`${this.baseUrl}/items`, { productId, quantity }));
  }

  async updateItem(productId: string, quantity: number): Promise<ShoppingCart> {
    return firstValueFrom(this.http.put<ShoppingCart>(`${this.baseUrl}/items/${productId}`, { quantity }));
  }

  async removeItem(productId: string): Promise<ShoppingCart> {
    return firstValueFrom(this.http.delete<ShoppingCart>(`${this.baseUrl}/items/${productId}`));
  }
}
