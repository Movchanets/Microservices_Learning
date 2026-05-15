// OrderDetailComponent unit tests.
// Verifies the order detail component creates correctly and injects the OrderStore.
// Full DOM rendering tests deferred due to OnPush + async signal interactions.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, Package, ChevronLeft, CheckCircle2 } from 'lucide-angular';
import { OrderDetailComponent } from './order-detail';
import { OrderStore } from '../order.store';

describe('OrderDetailComponent', () => {
  let component: OrderDetailComponent;
  let fixture: ComponentFixture<OrderDetailComponent>;

  const mockStore = {
    selectedOrder: signal<any>(null),
    loading: signal(false),
    error: signal<string | null>(null),
    loadOrderById: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({ Package, ChevronLeft, CheckCircle2 }),
        OrderDetailComponent,
      ],
      providers: [
        { provide: OrderStore, useValue: mockStore },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderDetailComponent);
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
