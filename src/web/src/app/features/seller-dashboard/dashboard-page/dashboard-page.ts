// Seller dashboard page component.
// Main container for the seller dashboard with sales overview and tab navigation.
// Shows a welcome/create-store screen when seller has no store yet.

import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { StoreSettingsStore } from '../store-settings.store';
import { SalesCardComponent } from '../components/sales-card/sales-card';

@Component({
  selector: 'app-seller-dashboard',

  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterOutlet, LucideAngularModule, SalesCardComponent],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-6xl">
        <div class="flex items-center justify-between mb-8">
          <h1 class="text-3xl font-bold text-foreground font-lexend" i18n>Seller Dashboard</h1>
          @if (settingsStore.hasSettings()) {
            <a routerLink="/seller/settings"
               class="p-2.5 rounded-xl hover:bg-muted/10 transition-colors">
              <lucide-icon name="Settings" class="w-5 h-5 text-muted"></lucide-icon>
            </a>
          }
        </div>

        @if (settingsStore.loading()) {
          <div class="flex justify-center p-12">
            <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
          </div>
        } @else if (!settingsStore.hasSettings()) {
          <!-- Welcome: Create Store -->
          <div class="max-w-xl mx-auto text-center py-16">
            <div class="w-20 h-20 bg-primary/10 rounded-3xl flex items-center justify-center mx-auto mb-6">
              <lucide-icon name="Store" class="w-10 h-10 text-primary"></lucide-icon>
            </div>
            <h2 class="text-2xl font-bold font-lexend mb-3" i18n>Create Your Store</h2>
            <p class="text-muted mb-8" i18n>Set up your store to start selling products on the marketplace.</p>

            @if (settingsStore.error()) {
              <div class="mb-4 p-3 bg-red-500/10 text-red-500 rounded-xl text-sm">{{ settingsStore.error() }}</div>
            }

            <div class="space-y-4 text-left">
              <div>
                <label class="block text-sm font-medium mb-1.5" i18n>Store Name</label>
                <input #storeNameInput [value]="storeName()" (input)="storeName.set(storeNameInput.value)"
                       placeholder="My Awesome Store"
                       data-testid="store-name-input"
                       class="w-full px-4 py-2.5 bg-card border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" />
                @if (storeName().length > 0 && storeName().trim().length < 2) {
                  <p class="text-xs text-red-500 mt-1" aria-live="polite">Store name must be at least 2 characters.</p>
                }
              </div>
              <div>
                <label class="block text-sm font-medium mb-1.5" i18n>Description</label>
                <textarea #storeDescInput [value]="storeDesc()" (input)="storeDesc.set(storeDescInput.value)"
                          placeholder="Tell customers what your store is about..."
                          rows="3"
                          data-testid="store-desc-input"
                          class="w-full px-4 py-2.5 bg-card border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"></textarea>
                @if (storeDesc().length > 0 && storeDesc().trim().length < 10) {
                  <p class="text-xs text-red-500 mt-1" aria-live="polite">Description must be at least 10 characters.</p>
                }
              </div>
              <button (click)="onCreateStore()"
                      [disabled]="createDisabled()"
                      data-testid="create-store-btn"
                      class="w-full px-6 py-3 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors cursor-pointer disabled:opacity-50">
                @if (settingsStore.loading()) {
                  <span i18n>Creating...</span>
                } @else {
                  <span i18n>Create Store</span>
                }
              </button>
            </div>
          </div>
        } @else {
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
        }
      </div>
    </div>
  `
})
export class SellerDashboardPageComponent implements OnInit {
  readonly settingsStore = inject(StoreSettingsStore);

  storeName = signal('');
  storeDesc = signal('');

  async ngOnInit(): Promise<void> {
    await this.settingsStore.loadSettings();
    if (this.settingsStore.hasSettings()) {
      this.settingsStore.loadSalesSummary();
    }
  }

  createDisabled(): boolean {
    return this.storeName().trim().length < 2
      || this.storeDesc().trim().length < 10
      || this.settingsStore.loading();
  }

  async onCreateStore(): Promise<void> {
    if (this.createDisabled()) return;
    await this.settingsStore.createStore(this.storeName().trim(), this.storeDesc().trim());
  }
}
