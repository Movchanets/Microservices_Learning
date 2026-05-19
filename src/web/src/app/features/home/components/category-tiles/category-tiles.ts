import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { Category } from '../../../catalog/catalog.models';

@Component({
  selector: 'app-category-tiles',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
      @for (category of categories(); track category.id) {
        <a
          [routerLink]="['/catalog']"
          [queryParams]="{ category: category.id }"
          class="group flex flex-col items-center gap-3 p-6 bg-card
                 border border-border rounded-2xl hover:border-primary/50 hover:bg-primary/5
                 transition-all"
        >
          <div class="w-12 h-12 flex items-center justify-center bg-primary/10 rounded-xl
                      group-hover:bg-primary/20 transition-colors">
            <lucide-icon name="Tag" class="w-6 h-6 text-primary"></lucide-icon>
          </div>
          <span class="text-sm font-medium text-foreground text-center group-hover:text-primary transition-colors">
            {{ category.name }}
          </span>
        </a>
      } @empty {
        @for (i of [1,2,3,4,5,6]; track i) {
          <div class="flex flex-col items-center gap-3 p-6 bg-muted/10 rounded-2xl animate-pulse">
            <div class="w-12 h-12 bg-muted/20 rounded-xl"></div>
            <div class="h-4 w-16 bg-muted/20 rounded"></div>
          </div>
        }
      }
    </div>
  `,
})
export class CategoryTilesComponent {
  categories = input.required<Category[]>();
}
