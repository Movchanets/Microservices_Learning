import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

interface SavedSearch {
  id: string;
  query: string;
  filtersJson: string;
  priceAlertEnabled: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-saved-searches',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LucideAngularModule],
  template: `
    <div class="space-y-4">
      <h2 class="text-xl font-bold text-foreground font-lexend flex items-center gap-2">
        <lucide-icon name="Bookmark" class="w-5 h-5"></lucide-icon>
        Saved Searches
      </h2>

      @if (loading()) {
        <div class="space-y-3 animate-pulse">
          @for (i of [1,2,3]; track i) {
            <div class="h-16 bg-muted/20 rounded-xl"></div>
          }
        </div>
      } @else if (searches().length === 0) {
        <div class="py-8 text-center text-muted-foreground">
          <lucide-icon name="Search" class="w-10 h-10 mx-auto mb-3 opacity-30"></lucide-icon>
          <p class="text-sm">No saved searches yet.</p>
          <p class="text-xs mt-1">Save a search from the catalog to see it here.</p>
        </div>
      } @else {
        <div class="space-y-2">
          @for (search of searches(); track search.id) {
            <div class="flex items-center justify-between p-4 bg-card border border-border rounded-xl
                        hover:border-primary/30 transition-colors">
              <div class="flex-1 min-w-0">
                <button
                  (click)="runSearch(search)"
                  class="text-left w-full"
                >
                  <p class="text-sm font-medium text-foreground truncate">{{ search.query || 'All products' }}</p>
                  <p class="text-xs text-muted-foreground mt-0.5">
                    Saved {{ search.createdAt | date:'mediumDate' }}
                    @if (search.priceAlertEnabled) {
                      <span class="ml-2 text-green-500">Price alerts on</span>
                    }
                  </p>
                </button>
              </div>
              <button
                (click)="deleteSearch(search.id)"
                class="p-2 text-muted-foreground hover:text-red-500 transition-colors"
                aria-label="Delete saved search"
              >
                <lucide-icon name="Trash2" class="w-4 h-4"></lucide-icon>
              </button>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class SavedSearchesComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);

  searches = signal<SavedSearch[]>([]);
  loading = signal(false);

  async ngOnInit(): Promise<void> {
    await this.loadSearches();
  }

  private async loadSearches(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.http.get<SavedSearch[]>('/api/identity/saved-searches'),
      );
      this.searches.set(result);
    } catch {
      this.searches.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  runSearch(search: SavedSearch): void {
    const params: Record<string, string> = {};
    if (search.query) params['q'] = search.query;

    try {
      const filters = JSON.parse(search.filtersJson);
      if (filters.categoryId) params['category'] = filters.categoryId;
      if (filters.priceMin) params['priceMin'] = filters.priceMin;
      if (filters.priceMax) params['priceMax'] = filters.priceMax;
      if (filters.brand) params['brand'] = filters.brand;
      if (filters.minRating) params['minRating'] = filters.minRating;
      if (filters.inStock) params['inStock'] = filters.inStock;
    } catch {
      // Ignore parse errors
    }

    this.router.navigate(['/catalog'], { queryParams: params });
  }

  async deleteSearch(id: string): Promise<void> {
    try {
      await firstValueFrom(
        this.http.delete(`/api/identity/saved-searches/${id}`),
      );
      this.searches.update(s => s.filter(item => item.id !== id));
    } catch {
      // Silently fail
    }
  }
}
