// NotificationService unit tests.
// Verifies the SignalR service creates correctly and has expected signals.
// Full connection tests are deferred — they require a running SignalR hub.

import { TestBed } from '@angular/core/testing';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NotificationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have initial state', () => {
    expect(service.orderUpdates()).toBeNull();
    expect(service.connected()).toBe(false);
  });

  it('should expose start method', () => {
    expect(typeof service.start).toBe('function');
  });

  it('should expose stop method', () => {
    expect(typeof service.stop).toBe('function');
  });
});
