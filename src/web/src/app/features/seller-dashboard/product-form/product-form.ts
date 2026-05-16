// Product form component.
// Handles both create and edit modes for seller products.
// Uses signals for form state, submits to SellerProductStore.

import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { SellerProductStore } from '../seller-product.store';
import { StoreSettingsStore } from '../store-settings.store';

@Component({
  selector: 'app-product-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <div class="bg-card/60 backdrop-blur-sm rounded-3xl border border-border p-6 max-w-2xl mx-auto">
      <h2 class="text-xl font-bold font-lexend mb-6">
        {{ isEditing() ? 'Edit Product' : 'Add Product' }}
      </h2>

      @if (store.error()) {
        <div class="mb-4 p-3 bg-red-500/10 text-red-500 rounded-xl text-sm">{{ store.error() }}</div>
      }

      <form (submit)="onSubmit($event)" class="space-y-5">
        <div>
          <label class="block text-sm font-medium mb-1.5" i18n>Product Name</label>
          <input [value]="name()" (input)="name.set($any($event.target).value)"
                 class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                 required />
        </div>

        <div>
          <label class="block text-sm font-medium mb-1.5" i18n>SKU</label>
          <input [value]="sku()" (input)="sku.set($any($event.target).value)"
                 class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                 [disabled]="isEditing()"
                 required />
        </div>

        <div>
          <label class="block text-sm font-medium mb-1.5" i18n>Description</label>
          <textarea [value]="description()" (input)="description.set($any($event.target).value)"
                    rows="3"
                    class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"></textarea>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Price</label>
            <input type="number" [value]="price()" (input)="price.set(+$any($event.target).value)"
                   min="0" step="0.01"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                   required />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Stock</label>
            <input type="number" [value]="stock()" (input)="stock.set(+$any($event.target).value)"
                   min="0"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
                   required />
          </div>
        </div>

        <div class="flex items-center gap-3 pt-2">
          <button type="submit"
                  [disabled]="store.loading()"
                  class="px-6 py-2.5 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors disabled:opacity-50 cursor-pointer">
            {{ store.loading() ? 'Saving...' : (isEditing() ? 'Update' : 'Create') }}
          </button>
          <a routerLink="/seller/products"
             class="px-6 py-2.5 text-muted hover:text-foreground transition-colors">
            Cancel
          </a>
        </div>
      </form>
    </div>
  `
})
export class ProductFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(SellerProductStore);
  private readonly storeSettingsStore = inject(StoreSettingsStore);

  isEditing = signal(false);
  productId = signal<string | null>(null);
  name = signal('');
  sku = signal('');
  description = signal('');
  price = signal(0);
  stock = signal(0);

  ngOnInit(): void {
    this.storeSettingsStore.loadSettings();
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEditing.set(true);
      this.productId.set(id);
      this.store.loadProductById(id);
    }
  }

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();

    if (this.isEditing()) {
      const success = await this.store.updateProduct(this.productId()!, {
        name: this.name(),
        description: this.description(),
        price: this.price(),
      });
      if (success) this.router.navigate(['/seller/products']);
    } else {
      const storeId = this.storeSettingsStore.settings()?.storeId || '';
      const success = await this.store.createProduct({
        name: this.name(),
        sku: this.sku(),
        description: this.description(),
        price: this.price(),
        currency: 'USD',
        categoryId: '',
        storeId,
      });
      if (success) this.router.navigate(['/seller/products']);
    }
  }
}
