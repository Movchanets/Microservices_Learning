import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    @if (totalPages() > 1) {
      <nav class="flex items-center justify-center gap-2 mt-8" aria-label="Pagination">
        <button
          (click)="pageChange.emit(currentPage() - 1)"
          [disabled]="!hasPrevious()"
          class="p-2 rounded-xl border border-border disabled:opacity-30 hover:bg-muted/20 transition-colors"
        >
          <lucide-icon name="ChevronLeft" class="w-5 h-5"></lucide-icon>
        </button>

        @for (p of visiblePages(); track p) {
          @if (p === -1) {
            <span class="px-2 text-muted">…</span>
          } @else {
            <button
              (click)="pageChange.emit(p)"
              class="min-w-[40px] h-10 rounded-xl text-sm font-medium transition-colors"
              [class.bg-primary]="p === currentPage()"
              [class.text-white]="p === currentPage()"
              [class.text-foreground]="p !== currentPage()"
              [class.hover:bg-muted/20]="p !== currentPage()"
            >
              {{ p }}
            </button>
          }
        }

        <button
          (click)="pageChange.emit(currentPage() + 1)"
          [disabled]="!hasNext()"
          class="p-2 rounded-xl border border-border disabled:opacity-30 hover:bg-muted/20 transition-colors"
        >
          <lucide-icon name="ChevronRight" class="w-5 h-5"></lucide-icon>
        </button>
      </nav>
    }
  `,
})
export class PaginationComponent {
  currentPage = input.required<number>();
  totalPages = input.required<number>();
  hasPrevious = input(false);
  hasNext = input(false);
  pageChange = output<number>();

  /** Show max 7 page buttons with ellipsis */
  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);

    const pages: number[] = [1];
    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);

    if (start > 2) pages.push(-1); // ellipsis
    for (let i = start; i <= end; i++) pages.push(i);
    if (end < total - 1) pages.push(-1); // ellipsis
    pages.push(total);

    return pages;
  });
}
