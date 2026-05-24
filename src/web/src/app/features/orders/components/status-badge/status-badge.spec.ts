// StatusBadgeComponent unit tests.
// Tests the color-coded status badge for all 9 OrderStatus values.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatusBadgeComponent } from './status-badge';

describe('StatusBadgeComponent', () => {
  let component: StatusBadgeComponent;
  let fixture: ComponentFixture<StatusBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatusBadgeComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(StatusBadgeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.componentRef.setInput('status', 'Submitted');
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display the status text', () => {
    fixture.componentRef.setInput('status', 'Completed');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Completed');
  });

  it('should apply correct class for Submitted', () => {
    fixture.componentRef.setInput('status', 'Submitted');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('blue');
  });

  it('should apply correct class for InventoryReserved', () => {
    fixture.componentRef.setInput('status', 'InventoryReserved');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('yellow');
  });

  it('should apply correct class for PaymentProcessing', () => {
    fixture.componentRef.setInput('status', 'PaymentProcessing');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('orange');
  });

  it('should apply correct class for Processing', () => {
    fixture.componentRef.setInput('status', 'Processing');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('purple');
  });

  it('should apply correct class for Shipped', () => {
    fixture.componentRef.setInput('status', 'Shipped');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('indigo');
  });

  it('should apply correct class for Delivered', () => {
    fixture.componentRef.setInput('status', 'Delivered');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('green');
  });

  it('should apply correct class for Completed', () => {
    fixture.componentRef.setInput('status', 'Completed');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('green');
  });

  it('should apply correct class for Cancelled', () => {
    fixture.componentRef.setInput('status', 'Cancelled');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('red');
  });

  it('should apply correct class for Faulted', () => {
    fixture.componentRef.setInput('status', 'Faulted');
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('red-700');
  });

  it('should apply default class for unknown status', () => {
    fixture.componentRef.setInput('status', 'Unknown' as any);
    fixture.detectChanges();

    const span = fixture.nativeElement.querySelector('span');
    expect(span.className).toContain('muted');
  });
});
