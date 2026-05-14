import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ShoppingCart, CheckoutResponse } from './cart.models';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/cart';

  // Helper to get buyer ID (mocked for now, normally from auth token/claims)
  private getHeaders(): HttpHeaders {
    // In a real app, this comes from the Identity store
    const buyerId = localStorage.getItem('buyerId') || 'guest-user';
    localStorage.setItem('buyerId', buyerId);
    return new HttpHeaders({ 'x-buyer-id': buyerId });
  }

  async getCart(): Promise<ShoppingCart> {
    return firstValueFrom(
      this.http.get<ShoppingCart>(this.baseUrl, { headers: this.getHeaders() }),
    );
  }

  async updateCart(cart: ShoppingCart): Promise<ShoppingCart> {
    return firstValueFrom(
      this.http.post<ShoppingCart>(this.baseUrl, cart, { headers: this.getHeaders() }),
    );
  }

  async deleteCart(): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.baseUrl, { headers: this.getHeaders() }));
  }

  async checkout(): Promise<CheckoutResponse> {
    return firstValueFrom(
      this.http.post<CheckoutResponse>(
        `${this.baseUrl}/checkout`,
        {},
        { headers: this.getHeaders() },
      ),
    );
  }
}
