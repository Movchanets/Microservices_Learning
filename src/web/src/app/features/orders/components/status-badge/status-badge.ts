import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { OrderStatus } from '../../../checkout/checkout.models';

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="badgeClass()">
      {{ status() }}
    </span>
  `,
})
export class StatusBadgeComponent {
  status = input.required<OrderStatus>();

  badgeClass(): string {
    const base = 'text-xs px-2.5 py-1 rounded-full font-medium';
    switch (this.status()) {
      case 'Submitted':
        return `${base} bg-blue-500/10 text-blue-500`;
      case 'InventoryReserved':
        return `${base} bg-yellow-500/10 text-yellow-500`;
      case 'PaymentProcessing':
        return `${base} bg-orange-500/10 text-orange-500`;
      case 'Completed':
        return `${base} bg-green-500/10 text-green-500`;
      case 'Processing':
        return `${base} bg-purple-500/10 text-purple-500`;
      case 'Shipped':
        return `${base} bg-indigo-500/10 text-indigo-500`;
      case 'Delivered':
        return `${base} bg-green-500/10 text-green-500`;
      case 'Cancelled':
        return `${base} bg-red-500/10 text-red-500`;
      case 'Faulted':
        return `${base} bg-red-700/10 text-red-700`;
      default:
        return `${base} bg-muted/10 text-muted`;
    }
  }
}
