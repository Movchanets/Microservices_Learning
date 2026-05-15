import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { DatePipe, SlicePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../../core/auth/auth.store';
import { Order } from '../../checkout/checkout.models';

@Component({
  selector: 'app-seller-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, SlicePipe, DecimalPipe, RouterLink, LucideAngularModule],
  template: `
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <h2 class="text-xl font-bold font-lexend text-foreground">Orders</h2>
      </div>

      @if (loading()) {
        <div class="flex justify-center p-12">
          <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
        </div>
      } @else if (orders().length === 0) {
        <div class="text-center py-16 bg-card/60 backdrop-blur-sm rounded-3xl border border-border">
          <lucide-icon name="Package" class="w-16 h-16 mx-auto mb-4 text-muted/30"></lucide-icon>
          <p class="text-xl font-medium text-foreground mb-2">No orders yet</p>
          <p class="text-muted">Orders containing your products will appear here</p>
        </div>
      } @else {
        <div class="bg-card/60 backdrop-blur-sm rounded-3xl border border-border overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-left text-muted">
                  <th class="p-4 font-medium">Order ID</th>
                  <th class="p-4 font-medium">Buyer</th>
                  <th class="p-4 font-medium">Status</th>
                  <th class="p-4 font-medium">Total</th>
                  <th class="p-4 font-medium">Date</th>
                </tr>
              </thead>
              <tbody>
                @for (order of orders(); track order.id) {
                  <tr class="border-b border-border/50 hover:bg-muted/5 transition-colors">
                    <td class="p-4 font-mono text-xs">{{ order.id | slice:0:8 }}...</td>
                    <td class="p-4">{{ order.buyerId | slice:0:8 }}...</td>
                    <td class="p-4">
                      <span [class]="statusClass(order.status)">{{ order.status }}</span>
                    </td>
                    <td class="p-4 font-medium">\${{ order.totalAmount | number:'1.2-2' }}</td>
                    <td class="p-4 text-muted">{{ order.createdAt | date:'short' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }
    </div>
  `
})
export class SellerOrdersComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authStore = inject(AuthStore);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void {
    const user = this.authStore.user();
    if (user) {
      this.loadOrders(user.id);
    }
  }

  private loadOrders(sellerId: string): void {
    this.loading.set(true);
    this.http.get<Order[]>(`/api/orders/seller/${sellerId}`).subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  statusClass(status: string): string {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold';
    const variants: Record<string, string> = {
      Submitted: `${base} bg-blue-500/10 text-blue-500`,
      InventoryReserved: `${base} bg-yellow-500/10 text-yellow-500`,
      PaymentProcessing: `${base} bg-yellow-500/10 text-yellow-500`,
      Completed: `${base} bg-green-500/10 text-green-500`,
      Cancelled: `${base} bg-red-500/10 text-red-500`,
      Faulted: `${base} bg-red-500/10 text-red-500`,
    };
    return variants[status] || base;
  }
}
