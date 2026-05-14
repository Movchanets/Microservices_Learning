import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { FacetValue } from '../../catalog.models';

@Component({
  selector: 'app-search-facets',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, LucideAngularModule],
  template: `
    <div class="bg-card/40 backdrop-blur-sm border border-border rounded-2xl p-5 space-y-6">
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
          <input type="number"
                 [ngModel]="priceMin()"
                 (ngModelChange)="onPriceChange($event, priceMax())"
                 placeholder="Min"
                 min="0"
                 class="w-full px-3 py-1.5 rounded-lg border border-border bg-background text-sm text-foreground"
                 data-testid="price-min" />
          <span class="text-muted text-xs">—</span>
          <input type="number"
                 [ngModel]="priceMax()"
                 (ngModelChange)="onPriceChange(priceMin(), $event)"
                 placeholder="Max"
                 min="0"
                 class="w-full px-3 py-1.5 rounded-lg border border-border bg-background text-sm text-foreground"
                 data-testid="price-max" />
        </div>
      </div>

      <!-- Category facets (from Search.API aggregations) -->
      @if (categoryFacets().length > 0) {
        <div>
          <h4 class="text-sm font-medium text-foreground mb-3">
            Categories
          </h4>
          <ul class="space-y-1.5">
            @for (facet of categoryFacets(); track facet.key) {
              <li>
                <button (click)="categoryClicked.emit(facet.key)"
                        class="w-full text-left px-3 py-1.5 rounded-lg hover:bg-muted/10 transition-colors text-sm flex justify-between items-center">
                  <span class="text-foreground">{{ facet.key }}</span>
                  <span class="text-xs text-muted bg-muted/10 px-2 py-0.5 rounded-full">{{ facet.count }}</span>
                </button>
              </li>
            }
          </ul>
        </div>
      }

      <!-- Clear filters -->
      <button (click)="clearFilters.emit()"
              class="w-full text-center text-sm text-primary hover:text-secondary transition-colors py-2">
        Clear all filters
      </button>
    </div>
  `
})
export class SearchFacetsComponent {
  categoryFacets = input<FacetValue[]>([]);
  priceMin = input<number | null>(null);
  priceMax = input<number | null>(null);

  priceRangeChange = output<{ min: number | null; max: number | null }>();
  categoryClicked = output<string>();
  clearFilters = output<void>();

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;

  onPriceChange(min: number | null, max: number | null): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      this.priceRangeChange.emit({ min: min || null, max: max || null });
    }, 500);
  }
}
