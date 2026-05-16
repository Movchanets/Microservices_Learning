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

  // TODO: Remove x-buyer-id header pattern — Cart API now uses JWT claims.
  //       All calls should go through withCredentials: true (handled by apiInterceptor).
  //       The buyerId is extracted from the JWT token on the server side.
  //       Ref: src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs
  //       Ref: src/web/src/app/core/http/api.interceptor.ts

  async getCart(): Promise<ShoppingCart> {
    return firstValueFrom(this.http.get<ShoppingCart>(this.baseUrl));
  }

  // TODO: Cart update now requires Price per item (Cart.API expects { sku, quantity, price }).
  //       The addToCart flow needs to pass the product price from the catalog.
  //       Ref: src/Microservices/Cart/Cart.Application/Commands/UpdateCartCommand.cs
  //       Ref: src/Microservices/Cart/Cart.Domain/Aggregates/CartItem.cs (has Price property)
  async updateCart(cart: ShoppingCart): Promise<ShoppingCart> {
    return firstValueFrom(this.http.post<ShoppingCart>(this.baseUrl, cart));
  }

  async deleteCart(): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.baseUrl));
  }

  async checkout(): Promise<CheckoutResponse> {
    return firstValueFrom(this.http.post<CheckoutResponse>(`${this.baseUrl}/checkout`, {}));
  }

  // TODO: Add single-item endpoints when Cart.API supports them.
  //       Currently Cart only supports full cart replacement (POST /api/cart).
  //       Needed for: "Add to Cart" button on product detail page.
  //       Backend change needed: POST /api/cart/items { sku, quantity, price }
  //       Ref: src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs
}
