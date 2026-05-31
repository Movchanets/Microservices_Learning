import { ComponentFixture, TestBed } from '@angular/core/testing';
import { importProvidersFrom } from '@angular/core';
import { LucideAngularModule, Minus, Plus, Loader, ShoppingCart, XCircle, AlertTriangle, CheckCircle } from 'lucide-angular';
import { BuyBoxComponent } from './buy-box';
import { CartStore } from '../../../cart/cart.store';
import { CartService } from '../../../cart/cart.service';

describe('BuyBoxComponent', () => {
  let component: BuyBoxComponent;
  let fixture: ComponentFixture<BuyBoxComponent>;
  let mockCartService: any;

  beforeEach(async () => {
    mockCartService = {
      getCart: vi.fn().mockResolvedValue({ buyerId: 'test', items: [] }),
      updateCart: vi.fn().mockResolvedValue(undefined),
      deleteCart: vi.fn().mockResolvedValue(undefined),
      checkout: vi.fn().mockResolvedValue({ correlationId: 'test' }),
      addItem: vi.fn().mockResolvedValue({ buyerId: 'test', items: [{ productId: 'SKU-1', storeId: 'store-1', quantity: 1, price: 10, lineTotal: 10 }] }),
      updateItem: vi.fn().mockResolvedValue({ buyerId: 'test', items: [] }),
      removeItem: vi.fn().mockResolvedValue({ buyerId: 'test', items: [] }),
    };

    await TestBed.configureTestingModule({
      imports: [BuyBoxComponent],
      providers: [
        { provide: CartService, useValue: mockCartService },
        importProvidersFrom(LucideAngularModule.pick({ Minus, Plus, Loader, ShoppingCart, XCircle, AlertTriangle, CheckCircle })),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BuyBoxComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('sku', 'SKU-1');
    fixture.componentRef.setInput('skuId', 'SKU-1-ID');
    fixture.componentRef.setInput('price', 29.99);
    fixture.componentRef.setInput('currency', 'USD');
    fixture.componentRef.setInput('productId', 'prod-1');
  });

  it('should create', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display the price', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('29.99');
  });

  it('should show out of stock button when stock is 0', () => {
    fixture.componentRef.setInput('stockQuantity', 0);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Out of Stock');
  });

  it('should disable add to cart button when out of stock', () => {
    fixture.componentRef.setInput('stockQuantity', 0);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('button');
    const addToCartBtn = Array.from(buttons).find((b: any) => b.textContent.includes('Out of Stock')) as HTMLButtonElement;
    expect(addToCartBtn.disabled).toBe(true);
  });

  it('should show quantity selector when stock is available', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Qty:');
  });

  it('should not show quantity selector when out of stock', () => {
    fixture.componentRef.setInput('stockQuantity', 0);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).not.toContain('Qty:');
  });

  it('should start with quantity of 1', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    expect(component.quantity()).toBe(1);
  });

  it('should increment quantity', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    component.increment();
    expect(component.quantity()).toBe(2);
  });

  it('should decrement quantity', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    component.increment();
    component.decrement();
    expect(component.quantity()).toBe(1);
  });

  it('should not decrement below 1', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    component.decrement();
    expect(component.quantity()).toBe(1);
  });

  it('should not increment above max quantity (stock)', () => {
    fixture.componentRef.setInput('stockQuantity', 3);
    fixture.detectChanges();

    component.increment();
    component.increment();
    component.increment(); // at 3 now, which is max
    component.increment(); // should not go to 4
    expect(component.quantity()).toBe(3);
  });

  it('should call cartStore.addToCart on add to cart click', async () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    await component.onAddToCart();

    expect(mockCartService.addItem).toHaveBeenCalledWith('prod-1', 'SKU-1-ID', 'SKU-1', 1);
  });

  it('should emit buyNow event', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    const emitSpy = vi.spyOn(component.buyNow, 'emit');
    component.onBuyNow();

    expect(emitSpy).toHaveBeenCalled();
  });
});
