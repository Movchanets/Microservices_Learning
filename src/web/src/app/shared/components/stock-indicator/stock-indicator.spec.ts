import { ComponentFixture, TestBed } from '@angular/core/testing';
import { importProvidersFrom } from '@angular/core';
import { LucideAngularModule, Loader, XCircle, AlertTriangle, CheckCircle } from 'lucide-angular';
import { StockIndicatorComponent } from './stock-indicator';

describe('StockIndicatorComponent', () => {
  let component: StockIndicatorComponent;
  let fixture: ComponentFixture<StockIndicatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockIndicatorComponent],
      providers: [importProvidersFrom(LucideAngularModule.pick({ Loader, XCircle, AlertTriangle, CheckCircle }))],
    }).compileComponents();

    fixture = TestBed.createComponent(StockIndicatorComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.componentRef.setInput('quantity', 10);
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should show loading state when loading is true', () => {
    fixture.componentRef.setInput('quantity', null);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Checking availability...');
  });

  it('should show loading state when quantity is null', () => {
    fixture.componentRef.setInput('quantity', null);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Checking availability...');
  });

  it('should show out of stock when quantity is 0', () => {
    fixture.componentRef.setInput('quantity', 0);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Out of Stock');
  });

  it('should show low stock warning when quantity is 1-4', () => {
    fixture.componentRef.setInput('quantity', 3);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Only 3 left in stock');
  });

  it('should show in stock when quantity is 5+', () => {
    fixture.componentRef.setInput('quantity', 10);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('In Stock');
  });

  it('should apply red color for out of stock', () => {
    fixture.componentRef.setInput('quantity', 0);
    fixture.detectChanges();

    const div = fixture.nativeElement.querySelector('div');
    expect(div.className).toContain('text-red-500');
  });

  it('should apply orange color for low stock', () => {
    fixture.componentRef.setInput('quantity', 2);
    fixture.detectChanges();

    const div = fixture.nativeElement.querySelector('div');
    expect(div.className).toContain('text-orange-500');
  });

  it('should apply green color for in stock', () => {
    fixture.componentRef.setInput('quantity', 10);
    fixture.detectChanges();

    const div = fixture.nativeElement.querySelector('div');
    expect(div.className).toContain('text-green-500');
  });
});
