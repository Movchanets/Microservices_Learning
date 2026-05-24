import { ComponentFixture, TestBed } from '@angular/core/testing';
import { importProvidersFrom } from '@angular/core';
import { LucideAngularModule, Check, X } from 'lucide-angular';
import { OrderTimelineComponent } from './order-timeline';
import { Order } from '../../checkout/checkout.models';

describe('OrderTimelineComponent', () => {
  let component: OrderTimelineComponent;
  let fixture: ComponentFixture<OrderTimelineComponent>;

  const baseOrder: Order = {
    id: 'order-1',
    buyerId: 'buyer-1',
    status: 'Submitted',
    totalAmount: 100,
    createdAt: '2026-01-01T00:00:00Z',
    completedAt: null,
    items: [],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderTimelineComponent],
      providers: [importProvidersFrom(LucideAngularModule.pick({ Check, X }))],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderTimelineComponent);
    component = fixture.componentInstance;
  });

  function setOrder(order: Partial<Order>) {
    fixture.componentRef.setInput('order', { ...baseOrder, ...order });
    fixture.detectChanges();
  }

  it('should create', () => {
    setOrder({});
    expect(component).toBeTruthy();
  });

  it('should have 4 steps', () => {
    setOrder({});
    expect(component.steps()).toHaveLength(4);
  });

  it('should mark Submitted as current when status is Submitted', () => {
    setOrder({ status: 'Submitted' });
    const steps = component.steps();
    expect(steps[0].current).toBe(true);
    expect(steps[0].completed).toBe(true);
    expect(steps[1].current).toBe(false);
  });

  it('should mark InventoryReserved correctly', () => {
    setOrder({ status: 'InventoryReserved' });
    const steps = component.steps();
    expect(steps[0].completed).toBe(true);
    expect(steps[0].current).toBe(false);
    expect(steps[1].current).toBe(true);
    expect(steps[1].completed).toBe(true);
  });

  it('should mark Completed as all completed', () => {
    setOrder({ status: 'Completed' });
    const steps = component.steps();
    steps.forEach(step => {
      expect(step.completed).toBe(true);
    });
    expect(steps[3].current).toBe(true);
  });

  it('should handle Cancelled status', () => {
    setOrder({ status: 'Cancelled' });
    const steps = component.steps();
    expect(steps[0].failed).toBe(false);
    expect(steps[0].completed).toBe(false);
  });

  it('should display step labels', () => {
    setOrder({});
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Submitted');
    expect(compiled.textContent).toContain('Reserved');
    expect(compiled.textContent).toContain('Payment');
    expect(compiled.textContent).toContain('Completed');
  });
});
