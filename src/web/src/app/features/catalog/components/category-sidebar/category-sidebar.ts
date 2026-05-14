import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Category } from '../../catalog.models';

@Component({
  selector: 'app-category-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="bg-card border border-border rounded-2xl p-6 sticky top-24">
      <h3 class="text-lg font-bold text-foreground font-lexend mb-4 flex items-center">
        <lucide-icon name="SlidersHorizontal" class="w-5 h-5 mr-2 text-primary"></lucide-icon>
        Categories
      </h3>

      <div class="space-y-1">
        <button (click)="categorySelected.emit(null)"
                class="w-full text-left px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
                [class.bg-primary]="!selectedId()"
                [class.text-white]="!selectedId()"
                [class.text-muted]="selectedId()"
                [class.hover:bg-muted/20]="selectedId()">
          All Products
        </button>

        @for (cat of categories(); track cat.id) {
          <button (click)="categorySelected.emit(cat.id)"
                  class="w-full text-left px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
                  [class.bg-primary]="selectedId() === cat.id"
                  [class.text-white]="selectedId() === cat.id"
                  [class.text-muted]="selectedId() !== cat.id"
                  [class.hover:bg-muted/20]="selectedId() !== cat.id">
            {{ cat.name }}
          </button>
        }
      </div>
    </div>
  `
})
export class CategorySidebarComponent {
  categories = input.required<Category[]>();
  selectedId = input<string | null>(null);
  categorySelected = output<string | null>();
}
