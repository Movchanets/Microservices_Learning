import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { DatePipe, SlicePipe, DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../../core/auth/auth.store';
import { OrderService } from '../../orders/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { Order } from '../../checkout/checkout.models';

@Component({
  selector: 'app-seller-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, SlicePipe, DecimalPipe, FormsModule, LucideAngularModule],
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
        <div class="text-center py-16 bg-card rounded-3xl border border-border">
          <lucide-icon name="Package" class="w-16 h-16 mx-auto mb-4 text-muted/30"></lucide-icon>
          <p class="text-xl font-medium text-foreground mb-2">No orders yet</p>
          <p class="text-muted">Orders containing your products will appear here</p>
        </div>
      } @else {
        <div class="bg-card rounded-3xl border border-border overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-left text-muted">
                  <th class="p-4 font-medium">Order ID</th>
                  <th class="p-4 font-medium">Buyer</th>
                  <th class="p-4 font-medium">Status</th>
                  <th class="p-4 font-medium">Total</th>
                  <th class="p-4 font-medium">Date</th>
                  <th class="p-4 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (order of orders(); track order.id) {
                  <tr class="border-b border-border/50 hover:bg-muted/5 transition-colors">
                    <td class="p-4 font-mono text-xs">{{ order.id | slice:0:8 }}...</td>
                    <td class="p-4">{{ order.buyerId | slice:0:8 }}...</td>
                    <td class="p-4">
                      <span data-testid="order-status-badge" [class]="statusClass(order.status)">{{ order.status }}</span>
                    </td>
                    <td class="p-4 font-medium">\${{ order.totalAmount | number:'1.2-2' }}</td>
                    <td class="p-4 text-muted">{{ order.createdAt | date:'short' }}</td>
                    <td class="p-4">
                      @if (getNextStatus(order.status); as next) {
                        @if (updatingId() === order.id) {
                          <div class="flex items-center gap-2">
                            <input
                              type="text"
                              [(ngModel)]="updateNotes"
                              placeholder="Notes (optional)"
                              class="px-2 py-1 text-xs bg-muted/10 border border-border rounded-lg w-32
                                     focus:outline-none focus:border-primary"
                            />
                            <button
                              (click)="confirmStatusUpdate(order.id, next)"
                              [disabled]="updating()"
                              class="px-2 py-1 bg-green-500/10 text-green-500 text-xs font-medium rounded-lg
                                     hover:bg-green-500/20 disabled:opacity-50"
                            >
                              @if (updating()) {
                                <lucide-icon name="Loader" class="w-3 h-3 animate-spin"></lucide-icon>
                              } @else {
                                OK
                              }
                            </button>
                            <button
                              (click)="updatingId.set(null)"
                              class="px-2 py-1 text-xs text-muted-foreground"
                            >
                              X
                            </button>
                          </div>
                        } @else {
                          <button
                            (click)="updatingId.set(order.id); updateNotes = ''"
                            class="px-3 py-1.5 bg-primary/10 text-primary text-xs font-medium rounded-lg
                                   hover:bg-primary/20 transition-colors flex items-center gap-1"
                          >
                            <lucide-icon name="ArrowRight" class="w-3 h-3"></lucide-icon>
                            Mark {{ next }}
                          </button>
                        }
                      }
                    </td>
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
  private readonly orderService = inject(OrderService);
  private readonly toast = inject(ToastService);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(false);
  updatingId = signal<string | null>(null);
  updateNotes = '';
  updating = signal(false);

  ngOnInit(): void {
    const user = this.authStore.user();
    if (user) {
      this.loadOrders(user.id);
    }
  }

  getNextStatus(currentStatus: string): string | null {
    switch (currentStatus) {
      case 'Submitted': return 'Processing';
      case 'Processing': return 'Shipped';
      case 'Shipped': return 'Delivered';
      default: return null;
    }
  }

  async confirmStatusUpdate(orderId: string, newStatus: string): Promise<void> {
    this.updating.set(true);
    try {
      await this.orderService.updateOrderStatus(orderId, newStatus, this.updateNotes || undefined);
      // Update local state
      this.orders.update(orders =>
        orders.map(o => o.id === orderId ? { ...o, status: newStatus as any } : o)
      );
      this.toast.success(`Order marked as ${newStatus}`);
      this.updatingId.set(null);
      this.updateNotes = '';
    } catch {
      this.toast.error('Failed to update order status');
    }
    this.updating.set(false);
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
      Processing: `${base} bg-purple-500/10 text-purple-500`,
      Shipped: `${base} bg-indigo-500/10 text-indigo-500`,
      Delivered: `${base} bg-green-500/10 text-green-500`,
      Completed: `${base} bg-green-500/10 text-green-500`,
      Cancelled: `${base} bg-red-500/10 text-red-500`,
      Faulted: `${base} bg-red-500/10 text-red-500`,
    };
    return variants[status] || base;
  }
}
