import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe, SlicePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { OrderStore } from '../order.store';
import { StatusBadgeComponent } from '../components/status-badge/status-badge';
import { AuthStore } from '../../../core/auth/auth.store';

@Component({
  selector: 'app-order-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CurrencyPipe, DatePipe, SlicePipe, LucideAngularModule, StatusBadgeComponent],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-4xl">
        <h1 class="text-3xl font-bold text-foreground font-lexend mb-8">My Orders</h1>

        @if (store.loading()) {
          <div class="flex justify-center p-12">
            <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
          </div>
        } @else if (store.error()) {
          <div class="p-4 bg-red-500/10 text-red-500 rounded-xl">{{ store.error() }}</div>
        } @else if (!store.hasOrders()) {
          <div class="text-center py-16 bg-card/60 backdrop-blur-sm rounded-3xl border border-border">
            <lucide-icon name="Package" class="w-16 h-16 mx-auto mb-4 opacity-30"></lucide-icon>
            <p class="text-xl font-medium text-foreground mb-4">No orders yet</p>
            <a routerLink="/catalog"
               class="inline-block px-6 py-3 bg-primary text-white rounded-xl hover:bg-secondary transition-colors">
              Start Shopping
            </a>
          </div>
        } @else {
          <!-- Active Orders -->
          @if (store.activeOrders().length > 0) {
            <h2 class="text-lg font-semibold text-foreground mb-4">Active Orders</h2>
            <div class="bg-card/60 backdrop-blur-sm rounded-3xl border border-border overflow-hidden mb-8">
              <ul class="divide-y divide-border">
                @for (order of store.activeOrders(); track order.id) {
                  <li>
                    <a [routerLink]="['/orders', order.id]"
                       class="p-5 flex items-center justify-between hover:bg-muted/5 transition-colors block">
                      <div class="flex items-center gap-4">
                        <div class="w-10 h-10 bg-primary/10 rounded-xl flex items-center justify-center">
                          <lucide-icon name="Package" class="w-5 h-5 text-primary"></lucide-icon>
                        </div>
                        <div>
                          <p class="font-medium text-sm font-mono">{{ order.id | slice:0:8 }}...</p>
                          <p class="text-xs text-muted">{{ order.createdAt | date:'short' }}</p>
                        </div>
                      </div>
                      <div class="flex items-center gap-4">
                        <app-status-badge [status]="order.status" />
                        <lucide-icon name="ChevronRight" class="w-4 h-4 text-muted"></lucide-icon>
                      </div>
                    </a>
                  </li>
                }
              </ul>
            </div>
          }

          <!-- Completed Orders -->
          @if (store.completedOrders().length > 0) {
            <h2 class="text-lg font-semibold text-foreground mb-4">Completed Orders</h2>
            <div class="bg-card/60 backdrop-blur-sm rounded-3xl border border-border overflow-hidden">
              <ul class="divide-y divide-border">
                @for (order of store.completedOrders(); track order.id) {
                  <li>
                    <a [routerLink]="['/orders', order.id]"
                       class="p-5 flex items-center justify-between hover:bg-muted/5 transition-colors block">
                      <div class="flex items-center gap-4">
                        <div class="w-10 h-10 bg-green-500/10 rounded-xl flex items-center justify-center">
                          <lucide-icon name="CheckCircle2" class="w-5 h-5 text-green-500"></lucide-icon>
                        </div>
                        <div>
                          <p class="font-medium text-sm font-mono">{{ order.id | slice:0:8 }}...</p>
                          <p class="text-xs text-muted">{{ order.createdAt | date:'short' }}</p>
                        </div>
                      </div>
                      <div class="flex items-center gap-4">
                        <span class="text-sm font-medium">{{ order.totalAmount | currency }}</span>
                        <app-status-badge [status]="order.status" />
                        <lucide-icon name="ChevronRight" class="w-4 h-4 text-muted"></lucide-icon>
                      </div>
                    </a>
                  </li>
                }
              </ul>
            </div>
          }
        }
      </div>
    </div>
  `,
})
export class OrderListComponent implements OnInit {
  readonly store = inject(OrderStore);
  private readonly authStore = inject(AuthStore);

  ngOnInit(): void {
    const buyerId = this.authStore.user()?.id || '';
    if (buyerId) {
      this.store.loadOrders(buyerId);
    }
  }
}
