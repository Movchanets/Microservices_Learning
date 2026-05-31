import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { InventoryStore, InventoryDisplayItem } from '../inventory.store';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-inventory-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, LucideAngularModule],
  template: `
    <div class="space-y-6">
      <!-- Low Stock Alert Banner -->
      @if (store.lowStockCount() > 0) {
        <div data-testid="low-stock-alert" class="p-4 bg-orange-500/10 border border-orange-500/20 rounded-xl flex items-center gap-3">
          <lucide-icon name="AlertTriangle" class="w-5 h-5 text-orange-500"></lucide-icon>
          <div class="flex-1">
            <p class="text-sm font-medium text-foreground">
              {{ store.lowStockCount() }} product{{ store.lowStockCount() > 1 ? 's' : '' }}
              {{ store.lowStockCount() > 1 ? 'are' : 'is' }} low on stock or out of stock.
            </p>
          </div>
          <button
            (click)="filter.set('low-stock')"
            class="px-3 py-1.5 text-sm font-medium text-orange-500 hover:bg-orange-500/10 rounded-lg transition-colors"
          >
            View Items
          </button>
        </div>
      }

      <!-- Filter Bar -->
      <div class="flex items-center gap-2">
        @for (f of filters; track f.value) {
          <button
            (click)="filter.set(f.value)"
            [class]="filter() === f.value
              ? 'px-4 py-2 bg-primary text-white text-sm font-medium rounded-lg'
              : 'px-4 py-2 bg-muted/10 text-foreground text-sm font-medium rounded-lg hover:bg-muted/20 transition-colors'"
          >
            {{ f.label }}
            @if (f.value !== 'all' && getCount(f.value) > 0) {
              <span class="ml-1.5 px-1.5 py-0.5 text-xs rounded-full"
                    [class]="f.value === 'out-of-stock' ? 'bg-red-500/20 text-red-400' : 'bg-orange-500/20 text-orange-400'">
                {{ getCount(f.value) }}
              </span>
            }
          </button>
        }
      </div>

      <!-- Inventory Table -->
      @if (store.loading()) {
        <div class="space-y-3 animate-pulse">
          @for (i of [1,2,3,4,5]; track i) {
            <div class="h-16 bg-muted/20 rounded-xl"></div>
          }
        </div>
      } @else if (store.error()) {
        <div class="py-12 text-center">
          <p class="text-red-400 mb-4">{{ store.error() }}</p>
          <button (click)="store.loadInventory()"
                  class="px-4 py-2 bg-primary text-white rounded-lg text-sm">
            Retry
          </button>
        </div>
      } @else if (filteredItems().length === 0) {
        <div class="py-12 text-center text-muted-foreground">
          <lucide-icon name="Package" class="w-12 h-12 mx-auto mb-3 opacity-30"></lucide-icon>
          <p>No inventory items found.</p>
        </div>
      } @else {
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b border-border">
                <th class="text-left py-3 px-4 text-sm font-medium text-muted-foreground">Product</th>
                <th class="text-left py-3 px-4 text-sm font-medium text-muted-foreground">SKU</th>
                <th class="text-left py-3 px-4 text-sm font-medium text-muted-foreground">Stock</th>
                <th class="text-left py-3 px-4 text-sm font-medium text-muted-foreground">Status</th>
                <th class="text-right py-3 px-4 text-sm font-medium text-muted-foreground">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (item of filteredItems(); track item.sku) {
                <tr class="border-b border-border/50 hover:bg-muted/5 transition-colors">
                  <td class="py-3 px-4">
                    <div class="flex items-center gap-3">
                      @if (item.imageUrl) {
                        <img [src]="item.imageUrl" class="w-10 h-10 rounded-lg object-cover" alt="" />
                      } @else {
                        <div class="w-10 h-10 bg-muted/20 rounded-lg flex items-center justify-center">
                          <lucide-icon name="Package" class="w-5 h-5 text-muted"></lucide-icon>
                        </div>
                      }
                      <span class="text-sm font-medium text-foreground">{{ item.productName }}</span>
                    </div>
                  </td>
                  <td class="py-3 px-4">
                    <span class="text-sm font-mono text-muted-foreground">{{ item.sku }}</span>
                  </td>
                  <td class="py-3 px-4">
                    <span class="text-sm font-medium text-foreground">{{ item.quantity }}</span>
                  </td>
                  <td class="py-3 px-4">
                    <span [class]="statusClass(item.status)"
                          class="px-2.5 py-1 text-xs font-medium rounded-full">
                      {{ statusLabel(item.status) }}
                    </span>
                  </td>
                  <td class="py-3 px-4 text-right">
                    @if (addingToSku() === item.sku) {
                      <div class="flex items-center justify-end gap-2">
                        <input
                          type="number"
                          [(ngModel)]="addQuantity"
                          min="1"
                          class="w-20 px-2 py-1 text-sm bg-muted/10 border border-border rounded-lg
                                 focus:outline-none focus:border-primary"
                          placeholder="Qty"
                        />
                        <button
                          (click)="confirmAddStock(item.sku)"
                          [disabled]="addingStock()"
                          class="px-3 py-1 bg-green-500/10 text-green-500 text-sm font-medium rounded-lg
                                 hover:bg-green-500/20 transition-colors disabled:opacity-50"
                        >
                          @if (addingStock()) {
                            <lucide-icon name="Loader" class="w-4 h-4 animate-spin"></lucide-icon>
                          } @else {
                            Confirm
                          }
                        </button>
                        <button
                          (click)="addingToSku.set(null)"
                          class="px-3 py-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
                        >
                          Cancel
                        </button>
                      </div>
                    } @else {
                      <button
                        (click)="addingToSku.set(item.sku); addQuantity = 1"
                        class="px-3 py-1.5 bg-primary/10 text-primary text-sm font-medium rounded-lg
                               hover:bg-primary/20 transition-colors flex items-center gap-1.5 ml-auto"
                      >
                        <lucide-icon name="Plus" class="w-4 h-4"></lucide-icon>
                        Add Stock
                      </button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class InventoryListComponent implements OnInit {
  protected store = inject(InventoryStore);
  private toast = inject(ToastService);

  filter = signal<'all' | 'low-stock' | 'out-of-stock'>('all');
  addingToSku = signal<string | null>(null);
  addQuantity = 1;
  addingStock = signal(false);

  filters = [
    { label: 'All Items', value: 'all' as const },
    { label: 'Low Stock', value: 'low-stock' as const },
    { label: 'Out of Stock', value: 'out-of-stock' as const },
  ];

  filteredItems = () => {
    const items = this.store.items();
    const f = this.filter();
    if (f === 'all') return items;
    return items.filter(i => i.status === f);
  };

  ngOnInit(): void {
    this.store.loadInventory();
  }

  getCount(status: string): number {
    return this.store.items().filter(i => i.status === status).length;
  }

  statusClass(status: string): string {
    switch (status) {
      case 'in-stock': return 'bg-green-500/10 text-green-500';
      case 'low-stock': return 'bg-orange-500/10 text-orange-500';
      case 'out-of-stock': return 'bg-red-500/10 text-red-500';
      default: return '';
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'in-stock': return 'In Stock';
      case 'low-stock': return 'Low Stock';
      case 'out-of-stock': return 'Out of Stock';
      default: return status;
    }
  }

  async confirmAddStock(sku: string): Promise<void> {
    if (this.addQuantity <= 0) return;
    this.addingStock.set(true);
    const success = await this.store.addStock(sku, this.addQuantity);
    if (success) {
      this.toast.success(`Added ${this.addQuantity} units to ${sku}`);
      this.addingToSku.set(null);
      this.addQuantity = 1;
    } else {
      this.toast.error('Failed to add stock');
    }
    this.addingStock.set(false);
  }
}
