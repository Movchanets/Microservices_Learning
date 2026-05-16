// Seller dashboard page component.
// Main container for the seller dashboard with sales overview and tab navigation.
// Loads sales summary on init.

import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { SellerProductStore } from '../seller-product.store';
import { StoreSettingsStore } from '../store-settings.store';
import { SalesCardComponent } from '../components/sales-card/sales-card';

@Component({
  selector: 'app-seller-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterOutlet, LucideAngularModule, SalesCardComponent],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-6xl">
        <div class="flex items-center justify-between mb-8">
          <h1 class="text-3xl font-bold text-foreground font-lexend" i18n>Seller Dashboard</h1>
          <a routerLink="/seller/settings"
             class="p-2.5 rounded-xl hover:bg-muted/10 transition-colors">
            <lucide-icon name="Settings" class="w-5 h-5 text-muted"></lucide-icon>
          </a>
        </div>

        <!-- Sales Overview -->
        <app-sales-card [summary]="settingsStore.salesSummary()" />

        <!-- Navigation Tabs -->
        <nav class="flex gap-1 mt-8 mb-6 bg-muted/10 rounded-xl p-1 w-fit">
          <a routerLink="/seller/products"
             class="px-4 py-2 rounded-lg text-sm font-medium hover:bg-background transition-colors"
             routerLinkActive="bg-background text-foreground shadow-sm"
             i18n>Products</a>
          <a routerLink="/seller/orders"
             class="px-4 py-2 rounded-lg text-sm font-medium hover:bg-background transition-colors"
             routerLinkActive="bg-background text-foreground shadow-sm"
             i18n>Orders</a>
          <a routerLink="/seller/inventory"
             class="px-4 py-2 rounded-lg text-sm font-medium hover:bg-background transition-colors"
             routerLinkActive="bg-background text-foreground shadow-sm"
             i18n>Inventory</a>
          <a routerLink="/seller/settings"
             class="px-4 py-2 rounded-lg text-sm font-medium hover:bg-background transition-colors"
             routerLinkActive="bg-background text-foreground shadow-sm"
             i18n>Settings</a>
        </nav>

        <!-- Content -->
        <router-outlet />
      </div>
    </div>
  `
})
export class SellerDashboardPageComponent implements OnInit {
  readonly productStore = inject(SellerProductStore);
  readonly settingsStore = inject(StoreSettingsStore);

  ngOnInit(): void {
    this.settingsStore.loadSalesSummary();
  }
}
