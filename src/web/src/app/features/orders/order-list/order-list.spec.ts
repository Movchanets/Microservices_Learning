// OrderListComponent unit tests.
// Verifies the order list component creates correctly and injects the OrderStore.
// DOM-level rendering tests are limited due to OnPush change detection with async stores.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, Package, ChevronRight, CheckCircle2 } from 'lucide-angular';
import { OrderListComponent } from './order-list';
import { OrderStore } from '../order.store';

describe('OrderListComponent', () => {
  let component: OrderListComponent;
  let fixture: ComponentFixture<OrderListComponent>;

  const mockStore = {
    orders: signal<any[]>([]),
    loading: signal(false),
    error: signal<string | null>(null),
    hasOrders: signal(false),
    activeOrders: signal<any[]>([]),
    completedOrders: signal<any[]>([]),
    loadOrders: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({ Package, ChevronRight, CheckCircle2 }),
        OrderListComponent,
      ],
      providers: [
        { provide: OrderStore, useValue: mockStore },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderListComponent);
    component = fixture.componentInstance;
    vi.clearAllMocks();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should inject OrderStore', () => {
    expect(component.store).toBeTruthy();
  });
});
