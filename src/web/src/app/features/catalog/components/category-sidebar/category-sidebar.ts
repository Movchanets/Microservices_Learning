import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Category } from '../../catalog.models';

@Component({
  selector: 'app-category-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <aside class="bg-card/40 backdrop-blur-sm border border-border rounded-2xl p-5">
      <h3 class="font-lexend font-semibold text-foreground mb-4 flex items-center gap-2">
        <lucide-icon name="SlidersHorizontal" class="w-4 h-4"></lucide-icon>
        Categories
      </h3>

      <ul class="space-y-1">
        <!-- All categories option -->
        <li>
          <button (click)="categorySelected.emit(null)"
                  class="w-full text-left px-3 py-2 rounded-xl transition-colors text-sm"
                  [class.bg-primary/10]="!selectedId()"
                  [class.text-primary]="!selectedId()"
                  [class.font-medium]="!selectedId()"
                  [class.text-foreground]="selectedId()">
            All Products
          </button>
        </li>

        @for (category of categories(); track category.id) {
          <li>
            <button (click)="categorySelected.emit(category.id)"
                    class="w-full text-left px-3 py-2 rounded-xl transition-colors text-sm"
                    [class.bg-primary/10]="selectedId() === category.id"
                    [class.text-primary]="selectedId() === category.id"
                    [class.font-medium]="selectedId() === category.id"
                    [class.text-foreground]="selectedId() !== category.id">
              {{ category.name }}
            </button>
          </li>
        }
      </ul>
    </aside>
  `
})
export class CategorySidebarComponent {
  categories = input.required<Category[]>();
  selectedId = input<string | null>(null);
  categorySelected = output<string | null>();
}
