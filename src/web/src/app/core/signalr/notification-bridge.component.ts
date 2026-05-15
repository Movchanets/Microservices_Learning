// Notification bridge component.
// Connects SignalR notifications to NgRx stores via effects.
// Renderless component — injects into app root to bridge real-time updates.

import { Component, effect, inject } from '@angular/core';
import { NotificationService } from './notification.service';
import { OrderStore } from '../../features/orders/order.store';
import { AuthStore } from '../auth/auth.store';

@Component({
  selector: 'app-notification-bridge',
  standalone: true,
  template: '',
})
export class NotificationBridgeComponent {
  private readonly notifications = inject(NotificationService);
  private readonly orderStore = inject(OrderStore);
  private readonly authStore = inject(AuthStore);

  constructor() {
    // When a SignalR OrderUpdate arrives, update the order store
    effect(() => {
      const update = this.notifications.orderUpdates();
      if (update) {
        this.orderStore.updateOrderStatus(update.orderId, update.status);
      }
    });

    // Stop SignalR connection when user logs out
    effect(() => {
      const user = this.authStore.user();
      if (!user) {
        this.notifications.stop();
      }
    });
  }
}
