import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { OrderStore } from '../order.store';
import { ToastService } from '../../../core/services/toast.service';
import { StatusBadgeComponent } from '../components/status-badge/status-badge';
import { OrderTimelineComponent } from '../order-timeline/order-timeline';

@Component({
  selector: 'app-order-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CurrencyPipe, DatePipe, FormsModule, LucideAngularModule, StatusBadgeComponent, OrderTimelineComponent],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-3xl">
        <a routerLink="/orders" class="text-muted hover:text-foreground transition-colors flex items-center gap-2 mb-6">
          <lucide-icon name="ChevronLeft" class="w-4 h-4"></lucide-icon>
          Back to Orders
        </a>

        @if (store.loading()) {
          <div class="flex justify-center p-12">
            <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
          </div>
        } @else if (store.selectedOrder(); as order) {
          <div class="bg-card rounded-3xl border border-border p-6 mb-6">
            <div class="flex items-center justify-between mb-4">
              <div>
                <h1 class="text-2xl font-bold font-lexend">Order Details</h1>
                <p class="text-sm text-muted font-mono mt-1">{{ order.id }}</p>
              </div>
              <div class="flex items-center gap-3">
                <app-status-badge [status]="order.status" />
                @if (canCancel(order.status)) {
                  @if (showCancelConfirm()) {
                    <div class="flex items-center gap-2">
                      <input
                        type="text"
                        [(ngModel)]="cancelReason"
                        placeholder="Reason (optional)"
                        class="px-3 py-1.5 text-sm bg-muted/10 border border-border rounded-lg
                               focus:outline-none focus:border-primary"
                      />
                      <button
                        (click)="confirmCancel(order.id)"
                        [disabled]="cancelling()"
                        class="px-3 py-1.5 bg-red-500/10 text-red-500 text-sm font-medium rounded-lg
                               hover:bg-red-500/20 transition-colors disabled:opacity-50"
                      >
                        @if (cancelling()) {
                          <lucide-icon name="Loader" class="w-4 h-4 animate-spin"></lucide-icon>
                        } @else {
                          Confirm
                        }
                      </button>
                      <button
                        (click)="showCancelConfirm.set(false)"
                        class="px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground"
                      >
                        Cancel
                      </button>
                    </div>
                  } @else {
                    <button
                      (click)="showCancelConfirm.set(true)"
                      class="px-4 py-2 bg-red-500/10 text-red-500 text-sm font-medium rounded-lg
                             hover:bg-red-500/20 transition-colors flex items-center gap-1.5"
                    >
                      <lucide-icon name="XCircle" class="w-4 h-4"></lucide-icon>
                      Cancel Order
                    </button>
                  }
                }
              </div>
            </div>

            <app-order-timeline [order]="order" class="mb-4" />

            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p class="text-muted">Created</p>
                <p data-testid="order-created-at" class="font-medium">{{ order.createdAt | date:'medium' }}</p>
              </div>
              @if (order.completedAt) {
                <div>
                  <p class="text-muted">Completed</p>
                  <p data-testid="order-completed-at" class="font-medium">{{ order.completedAt | date:'medium' }}</p>
                </div>
              }
              <div>
                <p class="text-muted">Total</p>
                <p data-testid="order-total" class="text-xl font-bold font-lexend">{{ order.totalAmount | currency }}</p>
              </div>
              <div>
                <p class="text-muted">Items</p>
                <p class="font-medium">{{ order.items.length }}</p>
              </div>
            </div>
          </div>

          <div class="bg-card rounded-3xl border border-border overflow-hidden">
            <div class="p-5 border-b border-border">
              <h2 class="font-semibold">Items</h2>
            </div>
            <ul class="divide-y divide-border">
              @for (item of order.items; track item.id) {
                <li class="p-5 flex items-center justify-between">
                  <div class="flex items-center gap-4">
                    <div class="w-10 h-10 bg-muted/20 rounded-lg flex items-center justify-center">
                      <lucide-icon name="Package" class="w-5 h-5 text-muted/50"></lucide-icon>
                    </div>
                    <div>
                      <p class="font-medium text-sm">{{ item.productName }}</p>
                      <p class="text-xs text-muted">{{ item.sku }} &middot; Qty: {{ item.quantity }}</p>
                    </div>
                  </div>
                  <div class="text-right">
                    <p class="font-medium">{{ item.totalPrice | currency }}</p>
                    <p class="text-xs text-muted">{{ item.unitPrice | currency }} each</p>
                  </div>
                </li>
              }
            </ul>
          </div>
        } @else if (store.error()) {
          <div class="p-4 bg-red-500/10 text-red-500 rounded-xl">{{ store.error() }}</div>
        }
      </div>
    </div>
  `,
})
export class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly store = inject(OrderStore);
  private readonly toast = inject(ToastService);

  showCancelConfirm = signal(false);
  cancelReason = '';
  cancelling = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.store.loadOrderById(id);
    }
  }

  canCancel(status: string): boolean {
    return ['Submitted', 'InventoryReserved', 'PaymentProcessing', 'Processing'].includes(status);
  }

  async confirmCancel(orderId: string): Promise<void> {
    this.cancelling.set(true);
    const success = await this.store.cancelOrder(orderId, this.cancelReason || undefined);
    if (success) {
      this.toast.success('Order cancelled successfully');
      this.showCancelConfirm.set(false);
      this.cancelReason = '';
    } else {
      this.toast.error('Failed to cancel order');
    }
    this.cancelling.set(false);
  }
}
