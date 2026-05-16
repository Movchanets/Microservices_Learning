import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ReviewSummary as ReviewSummaryType } from '../../catalog.models';

@Component({
  selector: 'app-review-summary',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    @if (summary(); as s) {
      <div class="flex flex-col gap-6">
        <!-- Average Rating -->
        <div class="flex items-center gap-4">
          <div class="text-5xl font-bold text-foreground font-lexend">
            {{ s.averageRating.toFixed(1) }}
          </div>
          <div class="flex flex-col gap-1">
            <div class="flex items-center gap-0.5">
              @for (star of stars(); track $index) {
                <lucide-icon
                  [name]="star ? 'Star' : 'Star'"
                  [class]="star ? 'w-5 h-5 text-yellow-400 fill-yellow-400' : 'w-5 h-5 text-muted'"
                  class="w-5 h-5"
                ></lucide-icon>
              }
            </div>
            <span class="text-sm text-muted-foreground">
              {{ s.totalReviews }} {{ s.totalReviews === 1 ? 'review' : 'reviews' }}
            </span>
          </div>
        </div>

        <!-- Rating Distribution Bars -->
        <div class="flex flex-col gap-2">
          @for (i of [5, 4, 3, 2, 1]; track i) {
            <button
              (click)="filterByRating.emit(i)"
              class="flex items-center gap-3 group hover:bg-muted/10 rounded-lg px-2 py-1 transition-colors"
            >
              <span class="text-sm text-muted-foreground w-3">{{ i }}</span>
              <lucide-icon name="Star" class="w-4 h-4 text-yellow-400 fill-yellow-400"></lucide-icon>
              <div class="flex-1 h-3 bg-muted/20 rounded-full overflow-hidden">
                <div
                  class="h-full bg-yellow-400 rounded-full transition-all"
                  [style.width.%]="barPercentage(i)"
                ></div>
              </div>
              <span class="text-sm text-muted-foreground w-8 text-right">
                {{ s.ratingDistribution[i] ?? 0 }}
              </span>
            </button>
          }
        </div>
      </div>
    }
  `,
})
export class ReviewSummaryComponent {
  summary = input.required<ReviewSummaryType>();
  filterByRating = output<number>();

  protected stars = computed(() => {
    const avg = this.summary()?.averageRating ?? 0;
    return Array.from({ length: 5 }, (_, i) => i < Math.round(avg));
  });

  protected barPercentage(rating: number): number {
    const s = this.summary();
    if (!s || s.totalReviews === 0) return 0;
    return ((s.ratingDistribution[rating] ?? 0) / s.totalReviews) * 100;
  }
}
