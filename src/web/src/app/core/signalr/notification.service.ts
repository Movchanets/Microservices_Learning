import { Injectable, signal } from '@angular/core';
import { OrderStatus } from '../../features/checkout/checkout.models';

export interface OrderUpdate {
  orderId: string;
  buyerId: string;
  status: OrderStatus;
  reason: string | null;
  timestamp: string;
}

/**
 * SignalR notification service.
 * Stubbed until Phase 5 (Notification.Worker) is fully wired.
 * Will connect to /hubs/notifications and receive real-time order updates.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly orderUpdates = signal<OrderUpdate | null>(null);
  readonly connected = signal(false);

  async start(): Promise<void> {
    // Stub — will be implemented with @microsoft/signalr
    // when Phase 5 Notification.Worker is running
    console.log('[NotificationService] Stub — SignalR not yet connected');
  }

  async stop(): Promise<void> {
    // Stub
  }
}
