import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CartStore } from '../../cart.store';

@Component({
  selector: 'app-mini-cart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <a
      routerLink="/cart"
      class="relative flex items-center justify-center p-2 rounded-xl hover:bg-muted/20 transition-colors"
      aria-label="Shopping Cart"
    >
      <lucide-icon name="ShoppingCart" class="w-6 h-6 text-foreground"></lucide-icon>

      @if (!store.isEmpty()) {
        <span
          class="absolute -top-1 -right-1 bg-primary text-white text-[10px] font-bold 
                     w-5 h-5 flex items-center justify-center rounded-full shadow-sm"
        >
          {{ store.totalItems() }}
        </span>
      }
    </a>
  `,
})
export class MiniCartComponent {
  store = inject(CartStore);
}
