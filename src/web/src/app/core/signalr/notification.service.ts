// SignalR notification service.
// Connects to /hubs/notifications via WebSocket for real-time order updates.
// Uses @microsoft/signalr with automatic reconnection.
// Receives OrderUpdate messages and exposes them as signals for store integration.

import { Injectable, signal, NgZone, inject } from '@angular/core';
import { HubConnectionBuilder, HubConnection, LogLevel, HttpTransportType } from '@microsoft/signalr';
import { OrderStatus } from '../../features/checkout/checkout.models';

export interface OrderUpdate {
  orderId: string;
  buyerId: string;
  status: OrderStatus;
  reason: string | null;
  timestamp: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly ngZone = inject(NgZone);
  private hubConnection: HubConnection | null = null;

  readonly orderUpdates = signal<OrderUpdate | null>(null);
  readonly connected = signal(false);

  async start(buyerId?: string): Promise<void> {
    if (this.hubConnection || !buyerId) {
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`/hubs/notifications?buyerId=${encodeURIComponent(buyerId)}`, {
        transport: HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 4s, 8s, 16s, then every 30s
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30_000);
        },
      })
      .configureLogging(LogLevel.Information)
      .build();

    // Handle OrderUpdate messages from the hub
    this.hubConnection.on('OrderUpdate', (message: OrderUpdate) => {
      this.ngZone.run(() => {
        this.orderUpdates.set(message);
      });
    });

    // Track connection state
    this.hubConnection.onreconnecting(() => {
      this.ngZone.run(() => this.connected.set(false));
    });

    this.hubConnection.onreconnected(() => {
      this.ngZone.run(() => this.connected.set(true));
    });

    this.hubConnection.onclose(() => {
      this.ngZone.run(() => this.connected.set(false));
    });

    try {
      await this.hubConnection.start();
      this.connected.set(true);
    } catch (err) {
      console.error('[NotificationService] Failed to connect:', err);
      this.connected.set(false);
    }
  }

  async stop(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
      this.connected.set(false);
    }
  }
}
