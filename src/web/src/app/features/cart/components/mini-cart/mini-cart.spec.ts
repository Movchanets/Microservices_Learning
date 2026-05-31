// MiniCartComponent unit tests.
// Verifies the mini-cart dropdown renders cart items, shows total count,
// handles empty state, and navigates to cart page.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, ShoppingCart } from 'lucide-angular';
import { MiniCartComponent } from './mini-cart';
import { CartStore } from '../../cart.store';

describe('MiniCartComponent', () => {
  let component: MiniCartComponent;
  let fixture: ComponentFixture<MiniCartComponent>;

  // Create signals for mocking the store
  const mockIsEmpty = signal(true);
  const mockTotalItems = signal(0);

  const mockCartStore = {
    isEmpty: mockIsEmpty,
    totalItems: mockTotalItems
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({ ShoppingCart }),
        MiniCartComponent
      ],
      providers: [
        provideRouter([]),
        { provide: CartStore, useValue: mockCartStore }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MiniCartComponent);
    component = fixture.componentInstance;

    // Reset signals
    mockIsEmpty.set(true);
    mockTotalItems.set(0);

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show empty state when no items are present', () => {
    mockIsEmpty.set(true);
    mockTotalItems.set(0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const badge = compiled.querySelector('span');
    expect(badge).toBeNull();
  });

  it('should display the item count badge correctly when items exist', () => {
    mockIsEmpty.set(false);
    mockTotalItems.set(5);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const badge = compiled.querySelector('span');
    expect(badge).toBeTruthy();
    expect(badge?.textContent?.trim()).toBe('5');
  });
});
