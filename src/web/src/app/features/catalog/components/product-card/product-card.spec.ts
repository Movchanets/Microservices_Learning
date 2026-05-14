import { vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductCardComponent } from './product-card';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, Tag, ShoppingCart, Package } from 'lucide-angular';
import { importProvidersFrom } from '@angular/core';
import { By } from '@angular/platform-browser';

describe('ProductCardComponent', () => {
  let component: ProductCardComponent;
  let fixture: ComponentFixture<ProductCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductCardComponent],
      providers: [
        provideRouter([]), // provide mock router for routerLink
        importProvidersFrom(LucideAngularModule.pick({ Tag, ShoppingCart, Package }))
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductCardComponent);
    component = fixture.componentInstance;
  });

  it('displays name, price (formatted), and "Add to Cart" button', () => {
    // Set the input signal
    fixture.componentRef.setInput('product', {
      id: 'p1',
      name: 'Test Product',
      sku: 'SKU-001',
      currency: 'USD',
      price: 1234.5,
      categoryName: 'Electronics'
    });

    fixture.detectChanges();

    const nameEl = fixture.debugElement.query(By.css('h3 a')).nativeElement;
    expect(nameEl.textContent).toContain('Test Product');

    // It uses currency pipe: 1234.5 | currency:'USD':'symbol':'1.2-2'
    // This typically renders as "$1,234.50"
    const priceEl = fixture.debugElement.query(By.css('.text-2xl')).nativeElement;
    expect(priceEl.textContent).toContain('$1,234.50');

    const addToCartBtn = fixture.debugElement.query(By.css('[data-testid="add-to-cart-btn"]'));
    expect(addToCartBtn).toBeTruthy();
  });

  it('navigates to details on image click and emits on Add to Cart', () => {
    fixture.componentRef.setInput('product', {
      id: 'p1',
      name: 'Test Product',
      sku: 'SKU-001',
      currency: 'USD',
      price: 1234.5,
      categoryName: 'Electronics'
    });

    fixture.detectChanges();

    // Check routerLink on image anchor
    const imageAnchor = fixture.debugElement.query(By.css('a.block'));
    expect(imageAnchor.attributes['href']).toBe('/p1');

    // Test output emission
    const emitSpy = vi.spyOn(component.addToCart, 'emit');
    const addToCartBtn = fixture.debugElement.query(By.css('[data-testid="add-to-cart-btn"]'));
    addToCartBtn.triggerEventHandler('click', null);

    expect(emitSpy).toHaveBeenCalledWith('p1');
  });
});
