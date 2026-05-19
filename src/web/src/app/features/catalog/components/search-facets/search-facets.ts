import { Component, ChangeDetectionStrategy, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { FacetValue } from '../../catalog.models';

@Component({
  selector: 'app-search-facets',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, LucideAngularModule],
  template: `
    <div class="bg-card border border-border rounded-2xl p-5 space-y-6">
      <h3 class="font-lexend font-semibold text-foreground flex items-center gap-2">
        <lucide-icon name="SlidersHorizontal" class="w-4 h-4"></lucide-icon>
        Filters
      </h3>

      <!-- Price range -->
      <div>
        <h4 class="text-sm font-medium text-foreground mb-3 flex items-center gap-1.5">
          <lucide-icon name="DollarSign" class="w-3.5 h-3.5 text-muted"></lucide-icon>
          Price Range
        </h4>
        <div class="flex gap-2 items-center">
          <input
            type="number"
            [ngModel]="priceMin()"
            (ngModelChange)="onPriceChange($event, priceMax())"
            placeholder="Min"
            min="0"
            class="w-full px-3 py-1.5 rounded-lg border border-border bg-background text-sm text-foreground"
          />
          <span class="text-muted text-xs">—</span>
          <input
            type="number"
            [ngModel]="priceMax()"
            (ngModelChange)="onPriceChange(priceMin(), $event)"
            placeholder="Max"
            min="0"
            class="w-full px-3 py-1.5 rounded-lg border border-border bg-background text-sm text-foreground"
          />
        </div>
      </div>

      <!-- Brand facets -->
      @if (brandFacets().length > 0) {
        <div>
          <h4 class="text-sm font-medium text-foreground mb-3">Brand</h4>
          <ul class="space-y-1.5 max-h-48 overflow-y-auto">
            @for (facet of brandFacets(); track facet.key) {
              <li>
                <label class="flex items-center gap-2 px-2 py-1 rounded-lg hover:bg-muted/10 cursor-pointer transition-colors">
                  <input
                    type="checkbox"
                    [checked]="selectedBrands().includes(facet.key)"
                    (change)="toggleBrand(facet.key)"
                    class="rounded border-border text-primary focus:ring-primary"
                  />
                  <span class="text-sm text-foreground flex-1">{{ facet.key }}</span>
                  <span class="text-xs text-muted">{{ facet.count }}</span>
                </label>
              </li>
            }
          </ul>
        </div>
      }

      <!-- Rating filter -->
      <div>
        <h4 class="text-sm font-medium text-foreground mb-3">Rating</h4>
        <div class="space-y-1.5">
          @for (rating of [4, 3, 2, 1]; track rating) {
            <button
              (click)="onRatingClick(rating)"
              [class]="selectedRating() === rating
                ? 'w-full text-left px-3 py-1.5 rounded-lg bg-primary/10 border border-primary/30 text-sm flex items-center gap-2'
                : 'w-full text-left px-3 py-1.5 rounded-lg hover:bg-muted/10 text-sm flex items-center gap-2 transition-colors'"
            >
              <div class="flex items-center gap-0.5">
                @for (i of [1,2,3,4,5]; track i) {
                  <lucide-icon
                    name="Star"
                    [class]="i <= rating ? 'w-3.5 h-3.5 text-yellow-400 fill-yellow-400' : 'w-3.5 h-3.5 text-muted'"
                  ></lucide-icon>
                }
              </div>
              <span class="text-foreground">& Up</span>
            </button>
          }
        </div>
      </div>

      <!-- In Stock toggle -->
      <div>
        <label class="flex items-center gap-3 px-2 py-2 rounded-lg hover:bg-muted/10 cursor-pointer transition-colors">
          <input
            type="checkbox"
            [checked]="inStockOnly()"
            (change)="inStockToggle.emit(!inStockOnly())"
            class="rounded border-border text-primary focus:ring-primary"
          />
          <span class="text-sm text-foreground">In Stock Only</span>
        </label>
      </div>

      <!-- Category facets -->
      @if (categoryFacets().length > 0) {
        <div>
          <h4 class="text-sm font-medium text-foreground mb-3">Categories</h4>
          <ul class="space-y-1.5">
            @for (facet of categoryFacets(); track facet.key) {
              <li>
                <button
                  (click)="categoryClicked.emit(facet.key)"
                  class="w-full text-left px-3 py-1.5 rounded-lg hover:bg-muted/10 transition-colors text-sm flex justify-between items-center"
                >
                  <span class="text-foreground">{{ facet.key }}</span>
                  <span class="text-xs text-muted bg-muted/10 px-2 py-0.5 rounded-full">{{ facet.count }}</span>
                </button>
              </li>
            }
          </ul>
        </div>
      }

      <!-- Clear filters -->
      <button
        (click)="clearFilters.emit()"
        class="w-full text-center text-sm text-primary hover:text-secondary transition-colors py-2"
      >
        Clear all filters
      </button>
    </div>
  `,
})
export class SearchFacetsComponent {
  categoryFacets = input<FacetValue[]>([]);
  brandFacets = input<FacetValue[]>([]);
  priceMin = input<number | null>(null);
  priceMax = input<number | null>(null);
  selectedBrands = input<string[]>([]);
  selectedRating = input<number | null>(null);
  inStockOnly = input(false);

  priceRangeChange = output<{ min: number | null; max: number | null }>();
  categoryClicked = output<string>();
  brandToggled = output<string>();
  ratingSelected = output<number | null>();
  inStockToggle = output<boolean>();
  clearFilters = output<void>();

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;

  onPriceChange(min: number | null, max: number | null): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      this.priceRangeChange.emit({ min: min || null, max: max || null });
    }, 500);
  }

  toggleBrand(brand: string): void {
    this.brandToggled.emit(brand);
  }

  onRatingClick(rating: number): void {
    this.ratingSelected.emit(this.selectedRating() === rating ? null : rating);
  }
}
