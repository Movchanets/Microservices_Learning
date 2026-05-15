// Sales card component.
// Displays sales metrics (orders, revenue, pending, completed) in a grid.
// Uses Lucide icons for visual indicators.

import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { SalesSummary } from '../../seller.models';

@Component({
  selector: 'app-sales-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, LucideAngularModule],
  template: `
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <div class="bg-card/60 backdrop-blur-sm rounded-2xl border border-border p-5">
        <lucide-icon name="ShoppingBag" class="w-5 h-5 text-primary mb-2"></lucide-icon>
        <p class="text-2xl font-bold font-lexend">{{ summary()?.totalOrders ?? 0 }}</p>
        <p class="text-xs text-muted" i18n>Total Orders</p>
      </div>
      <div class="bg-card/60 backdrop-blur-sm rounded-2xl border border-border p-5">
        <lucide-icon name="DollarSign" class="w-5 h-5 text-success mb-2"></lucide-icon>
        <p class="text-2xl font-bold font-lexend">{{ summary()?.totalRevenue ?? 0 | currency }}</p>
        <p class="text-xs text-muted" i18n>Revenue</p>
      </div>
      <div class="bg-card/60 backdrop-blur-sm rounded-2xl border border-border p-5">
        <lucide-icon name="Clock" class="w-5 h-5 text-yellow-500 mb-2"></lucide-icon>
        <p class="text-2xl font-bold font-lexend">{{ summary()?.pendingOrders ?? 0 }}</p>
        <p class="text-xs text-muted" i18n>Pending</p>
      </div>
      <div class="bg-card/60 backdrop-blur-sm rounded-2xl border border-border p-5">
        <lucide-icon name="CheckCircle2" class="w-5 h-5 text-green-500 mb-2"></lucide-icon>
        <p class="text-2xl font-bold font-lexend">{{ summary()?.completedOrders ?? 0 }}</p>
        <p class="text-xs text-muted" i18n>Completed</p>
      </div>
    </div>
  `
})
export class SalesCardComponent {
  summary = input<SalesSummary | null>(null);
}
