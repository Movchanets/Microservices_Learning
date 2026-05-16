import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Order, PaymentStatus } from '../checkout/checkout.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  async getOrderById(orderId: string): Promise<Order> {
    return firstValueFrom(this.http.get<Order>(`/api/orders/${orderId}`));
  }

  async getOrdersByBuyer(buyerId: string): Promise<Order[]> {
    return firstValueFrom(this.http.get<Order[]>(`/api/orders/buyer/${buyerId}`));
  }

  async getPaymentStatus(orderId: string): Promise<PaymentStatus> {
    return firstValueFrom(this.http.get<PaymentStatus>(`/api/payments/order/${orderId}`));
  }

  // TODO: Add cancelOrder method — backend endpoint exists at POST /api/orders/{id}/cancel
  //       Ref: src/Microservices/Ordering/Ordering.API/Endpoints/OrderEndpoints.cs
  //       The CancelOrderCommand handler already exists in Ordering.Application.
  //       Frontend needs: cancel button on order detail page (for orders with status Submitted/InventoryReserved).
  // async cancelOrder(orderId: string): Promise<void> {
  //   return firstValueFrom(this.http.post<void>(`/api/orders/${orderId}/cancel`, {}));
  // }

  // TODO: Add order address to checkout flow.
  //       OrderSubmittedEvent now expects shipping address fields.
  //       Ref: src/BuildingBlocks/SharedContracts/Events/Cart/OrderSubmittedEvent.cs
  //       Frontend needs: address form in checkout page (street, city, postalCode, country).
  //       Ref: plans/future_design/cart_and_checkout.md — "Single-Page Checkout" section
}
