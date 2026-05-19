// Seller product list component.
// Displays all products for the current seller with edit/delete actions.
// Loads products from SellerProductStore on init.

import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { SellerProductStore } from '../seller-product.store';
import { SellerProduct } from '../seller.models';

@Component({
  selector: 'app-seller-product-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CurrencyPipe, LucideAngularModule],
  template: `
    <div class="bg-card rounded-3xl border border-border overflow-hidden">
      <div class="p-6 flex items-center justify-between border-b border-border">
        <h2 class="text-xl font-bold font-lexend" i18n>My Products</h2>
        <a routerLink="/seller/products/new"
           class="px-4 py-2 bg-primary text-white rounded-xl text-sm font-medium hover:bg-secondary transition-colors">
          <span i18n>+ Add Product</span>
        </a>
      </div>

      @if (store.loading()) {
        <div class="flex justify-center p-12">
          <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
        </div>
      } @else if (store.error()) {
        <div class="p-4 m-4 bg-red-500/10 text-red-500 rounded-xl">{{ store.error() }}</div>
      } @else if (!store.hasProducts()) {
        <div class="text-center py-12">
          <lucide-icon name="Package" class="w-12 h-12 mx-auto mb-3 opacity-30"></lucide-icon>
          <p class="text-muted" i18n>No products yet</p>
        </div>
      } @else {
        <ul class="divide-y divide-border">
          @for (product of store.products(); track product.id) {
            <li class="p-4 flex items-center justify-between hover:bg-muted/5 transition-colors">
              <div class="flex items-center gap-4">
                <div class="w-12 h-12 bg-muted/20 rounded-xl flex items-center justify-center">
                  <lucide-icon name="Package" class="w-6 h-6 text-muted/50"></lucide-icon>
                </div>
                <div>
                  <p class="font-medium">{{ product.name }}</p>
                  <p class="text-sm text-muted">{{ product.sku }} &middot; {{ product.price | currency }}</p>
                </div>
              </div>
              <div class="flex items-center gap-2">
                @if (product.status !== 'Active') {
                  <span class="text-xs px-2 py-1 bg-yellow-500/10 text-yellow-500 rounded-full">{{ product.status }}</span>
                }
                <a [routerLink]="['/seller/products', product.id, 'edit']"
                   class="p-2 rounded-lg hover:bg-muted/10 transition-colors">
                  <lucide-icon name="Pencil" class="w-4 h-4 text-muted"></lucide-icon>
                </a>
                <button (click)="onDelete(product)"
                        class="p-2 rounded-lg hover:bg-red-500/10 transition-colors cursor-pointer">
                  <lucide-icon name="Trash2" class="w-4 h-4 text-red-500"></lucide-icon>
                </button>
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `
})
export class SellerProductListComponent implements OnInit {
  readonly store = inject(SellerProductStore);

  ngOnInit(): void {
    this.store.loadProducts();
  }

  async onDelete(product: SellerProduct): Promise<void> {
    if (confirm(`Delete "${product.name}"?`)) {
      await this.store.deleteProduct(product.id);
    }
  }
}
