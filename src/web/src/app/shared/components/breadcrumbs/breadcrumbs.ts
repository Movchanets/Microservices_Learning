import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

export interface BreadcrumbItem {
  label: string;
  link?: string;
}

@Component({
  selector: 'app-breadcrumbs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <nav aria-label="Breadcrumb" class="flex items-center gap-1.5 text-sm text-muted-foreground">
      <a routerLink="/" class="hover:text-primary transition-colors">
        <lucide-icon name="Home" class="w-4 h-4"></lucide-icon>
      </a>
      @for (item of items(); track item.label; let last = $last) {
        <lucide-icon name="ChevronRight" class="w-3.5 h-3.5"></lucide-icon>
        @if (item.link && !last) {
          <a [routerLink]="item.link" class="hover:text-primary transition-colors">
            {{ item.label }}
          </a>
        } @else {
          <span [class]="last ? 'text-foreground font-medium' : ''">{{ item.label }}</span>
        }
      }
    </nav>
  `,
})
export class BreadcrumbsComponent {
  items = input.required<BreadcrumbItem[]>();
}
