// Notification bridge component.
// Connects SignalR notifications to NgRx stores via effects.
// Renderless component — injects into app root to bridge real-time updates.

import { Component, ChangeDetectionStrategy, effect, inject, untracked } from '@angular/core';
import { NotificationService } from './notification.service';
import { OrderStore } from '../../features/orders/order.store';
import { AuthStore } from '../auth/auth.store';

@Component({
  selector: 'app-notification-bridge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '',
})
export class NotificationBridgeComponent {
  private readonly notifications = inject(NotificationService);
  private readonly orderStore = inject(OrderStore);
  private readonly authStore = inject(AuthStore);

  constructor() {
    // When a SignalR OrderUpdate arrives, update the order store.
    // CRITICAL: updateOrderStatus() internally reads store.orders() and
    // creates a new array via .map(). Without untracked(), Angular tracks
    // that read as an effect dependency — patchState creates a new array
    // reference → signal fires → effect re-triggers → infinite loop → UI freeze.
    effect(() => {
      const update = this.notifications.orderUpdates();
      if (update) {
        untracked(() => {
          this.orderStore.updateOrderStatus(update.orderId, update.status);
        });
      }
    });

    // Stop SignalR connection when user logs out
    effect(() => {
      const user = this.authStore.user();
      if (!user) {
        untracked(() => {
          this.notifications.stop();
        });
      }
    });
  }
}
