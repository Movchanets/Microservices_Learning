import { Component, ChangeDetectionStrategy, input, output, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CatalogService } from '../../../features/catalog/catalog.service';
import { ProductListItem } from '../../../features/catalog/catalog.models';

@Component({
  selector: 'app-search-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, FormsModule, LucideAngularModule],
  template: `
    <div class="relative" (clickOutside)="closeDropdown()">
      <div class="relative flex items-center">
        <input
          type="text"
          [ngModel]="query()"
          (ngModelChange)="onQueryChange($event)"
          (keydown.enter)="onSearch()"
          (focus)="onFocus()"
          placeholder="Search products..."
          class="w-full px-4 py-2.5 pl-10 pr-20 bg-muted/10 border border-border rounded-xl
                 text-foreground placeholder:text-muted-foreground
                 focus:outline-none focus:border-primary transition-colors"
        />
        <lucide-icon name="Search" class="absolute left-3 w-4 h-4 text-muted"></lucide-icon>
        <button
          (click)="onSearch()"
          class="absolute right-2 px-3 py-1 bg-primary text-white text-sm font-medium rounded-lg
                 hover:bg-secondary transition-colors"
        >
          Search
        </button>
      </div>

      <!-- Dropdown -->
      @if (showDropdown() && (suggestions().length > 0 || recentSearches().length > 0)) {
        <div class="absolute top-full left-0 right-0 mt-2 bg-card border border-border
                    rounded-xl shadow-lg overflow-hidden z-50">
          <!-- Suggestions -->
          @if (suggestions().length > 0) {
            <div class="p-2">
              <span class="px-3 py-1 text-xs text-muted-foreground font-medium">Suggestions</span>
              @for (product of suggestions(); track product.id) {
                <button
                  (click)="selectSuggestion(product)"
                  class="w-full text-left px-3 py-2 rounded-lg hover:bg-muted/10 transition-colors
                         flex items-center gap-3"
                >
                  @if (product.imageUrl) {
                    <img [src]="product.imageUrl" class="w-8 h-8 rounded object-cover" alt="" />
                  } @else {
                    <div class="w-8 h-8 bg-muted/20 rounded flex items-center justify-center">
                      <lucide-icon name="Package" class="w-4 h-4 text-muted"></lucide-icon>
                    </div>
                  }
                  <div class="flex-1 min-w-0">
                    <p class="text-sm text-foreground truncate">{{ product.name }}</p>
                    <p class="text-xs text-muted-foreground">{{ product.categoryName }}</p>
                  </div>
                  <span class="text-sm font-medium text-foreground">{{ product.price | number:'1.2-2' }}</span>
                </button>
              }
            </div>
          }

          <!-- Recent Searches -->
          @if (recentSearches().length > 0 && suggestions().length === 0) {
            <div class="p-2 border-t border-border">
              <div class="flex items-center justify-between px-3 py-1">
                <span class="text-xs text-muted-foreground font-medium">Recent Searches</span>
                <button
                  (click)="clearRecent(); $event.stopPropagation()"
                  class="text-xs text-primary hover:text-secondary"
                >
                  Clear
                </button>
              </div>
              @for (search of recentSearches(); track search) {
                <button
                  (click)="selectRecent(search)"
                  class="w-full text-left px-3 py-2 rounded-lg hover:bg-muted/10 transition-colors
                         flex items-center gap-2"
                >
                  <lucide-icon name="Clock" class="w-3.5 h-3.5 text-muted"></lucide-icon>
                  <span class="text-sm text-foreground">{{ search }}</span>
                </button>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class SearchBarComponent implements OnInit, OnDestroy {
  initialQuery = input('');

  search = output<string>();

  private router = inject(Router);
  private catalogService = inject(CatalogService);

  query = signal('');
  suggestions = signal<ProductListItem[]>([]);
  recentSearches = signal<string[]>([]);
  showDropdown = signal(false);

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private static readonly STORAGE_KEY = 'recentSearches';
  private static readonly MAX_RECENT = 5;

  ngOnInit(): void {
    this.query.set(this.initialQuery());
    this.loadRecentSearches();
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
  }

  onQueryChange(value: string): void {
    this.query.set(value);
    if (this.debounceTimer) clearTimeout(this.debounceTimer);

    if (value.trim().length < 2) {
      this.suggestions.set([]);
      return;
    }

    this.debounceTimer = setTimeout(async () => {
      try {
        const result = await this.catalogService.searchProducts({
          q: value.trim(),
          pageSize: 5,
        });
        this.suggestions.set(result.items);
        this.showDropdown.set(true);
      } catch {
        this.suggestions.set([]);
      }
    }, 300);
  }

  onSearch(): void {
    const q = this.query().trim();
    if (!q) return;

    this.saveRecentSearch(q);
    this.showDropdown.set(false);
    this.suggestions.set([]);
    this.search.emit(q);
    this.router.navigate(['/catalog'], { queryParams: { q } });
  }

  selectSuggestion(product: ProductListItem): void {
    this.showDropdown.set(false);
    this.suggestions.set([]);
    this.router.navigate(['/catalog', product.id]);
  }

  selectRecent(search: string): void {
    this.query.set(search);
    this.onSearch();
  }

  onFocus(): void {
    if (this.query().trim().length >= 2) {
      this.showDropdown.set(true);
    } else if (this.recentSearches().length > 0) {
      this.showDropdown.set(true);
    }
  }

  closeDropdown(): void {
    this.showDropdown.set(false);
  }

  clearRecent(): void {
    this.recentSearches.set([]);
    try {
      localStorage.removeItem(SearchBarComponent.STORAGE_KEY);
    } catch {
      // SSR-safe
    }
  }

  private loadRecentSearches(): void {
    try {
      const stored = localStorage.getItem(SearchBarComponent.STORAGE_KEY);
      if (stored) {
        this.recentSearches.set(JSON.parse(stored));
      }
    } catch {
      // SSR-safe
    }
  }

  private saveRecentSearch(query: string): void {
    const current = this.recentSearches().filter(s => s !== query);
    const updated = [query, ...current].slice(0, SearchBarComponent.MAX_RECENT);
    this.recentSearches.set(updated);
    try {
      localStorage.setItem(SearchBarComponent.STORAGE_KEY, JSON.stringify(updated));
    } catch {
      // SSR-safe
    }
  }
}
