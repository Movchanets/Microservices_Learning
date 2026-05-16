import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-stock-indicator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div [class]="containerClass()" class="flex items-center gap-2 text-sm font-medium">
      <lucide-icon [name]="iconName()" [class]="iconClass()" class="w-4 h-4"></lucide-icon>
      <span>{{ label() }}</span>
    </div>
  `,
})
export class StockIndicatorComponent {
  quantity = input.required<number | null>();
  loading = input(false);

  protected iconName = computed(() => {
    if (this.loading() || this.quantity() === null) return 'Loader';
    if (this.quantity() === 0) return 'XCircle';
    if (this.quantity()! < 5) return 'AlertTriangle';
    return 'CheckCircle';
  });

  protected label = computed(() => {
    if (this.loading() || this.quantity() === null) return 'Checking availability...';
    const qty = this.quantity()!;
    if (qty === 0) return 'Out of Stock';
    if (qty < 5) return `Only ${qty} left in stock`;
    return 'In Stock';
  });

  protected containerClass = computed(() => {
    if (this.loading() || this.quantity() === null) return 'text-muted-foreground';
    const qty = this.quantity()!;
    if (qty === 0) return 'text-red-500';
    if (qty < 5) return 'text-orange-500';
    return 'text-green-500';
  });

  protected iconClass = computed(() => {
    if (this.loading() || this.quantity() === null) return 'animate-spin';
    return '';
  });
}
