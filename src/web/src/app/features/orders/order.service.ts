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
}
