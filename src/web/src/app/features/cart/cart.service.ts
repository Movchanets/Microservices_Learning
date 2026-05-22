import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ShoppingCart, CheckoutResponse } from './cart.models';

const CART_ID_KEY = 'anon_cart_id';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly baseUrl = '/api/cart';
  private readonly bffCartUrl = '/bff/cart';

  /**
   * Returns stored anonymous cart ID from localStorage, or null.
   * Guards against SSR where localStorage is unavailable.
   */
  getCartId(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    try {
      return localStorage.getItem(CART_ID_KEY);
    } catch {
      return null;
    }
  }

  /**
   * Persists anonymous cart ID to localStorage.
   */
  setCartId(cartId: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(CART_ID_KEY, cartId);
    } catch {
      // Storage quota exceeded — ignore
    }
  }

  /**
   * Clears stored anonymous cart ID (e.g. after login/merge).
   */
  clearCartId(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.removeItem(CART_ID_KEY);
    } catch {
      // ignore
    }
  }

  /**
   * Builds headers with X-Cart-Id for anonymous cart identification.
   */
  private cartHeaders(): HttpHeaders {
    const cartId = this.getCartId();
    if (cartId) {
      return new HttpHeaders({ 'X-Cart-Id': cartId });
    }
    return new HttpHeaders();
  }

  async getCart(): Promise<ShoppingCart> {
    return firstValueFrom(
      this.http.get<ShoppingCart>(this.bffCartUrl, { headers: this.cartHeaders() })
    );
  }

  async deleteCart(): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(this.baseUrl, { headers: this.cartHeaders() })
    );
  }

  async checkout(address?: { addressLine1: string; city: string; state: string; postalCode: string; country: string }): Promise<CheckoutResponse> {
    return firstValueFrom(this.http.post<CheckoutResponse>(`${this.baseUrl}/checkout`, address || {}, { headers: this.cartHeaders() }));
  }

  async addItem(productId: string, quantity: number): Promise<ShoppingCart> {
    return firstValueFrom(
      this.http.post<ShoppingCart>(`${this.baseUrl}/items`, { productId, quantity }, { headers: this.cartHeaders() })
    );
  }

  async updateItem(productId: string, quantity: number): Promise<ShoppingCart> {
    return firstValueFrom(
      this.http.put<ShoppingCart>(`${this.baseUrl}/items/${productId}`, { quantity }, { headers: this.cartHeaders() })
    );
  }

  async removeItem(productId: string): Promise<ShoppingCart> {
    return firstValueFrom(
      this.http.delete<ShoppingCart>(`${this.baseUrl}/items/${productId}`, { headers: this.cartHeaders() })
    );
  }
}
