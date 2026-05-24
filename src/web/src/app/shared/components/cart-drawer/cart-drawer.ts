import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, X, Plus, Minus, Trash2, Package } from 'lucide-angular';
import { CartStore } from '../../../features/cart/cart.store';

@Component({
  selector: 'app-cart-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, RouterLink, LucideAngularModule],
  templateUrl: './cart-drawer.html',
  styleUrl: './cart-drawer.css',
})
export class CartDrawer {
  cartStore = inject(CartStore);

  readonly XIcon = X;
  readonly PlusIcon = Plus;
  readonly MinusIcon = Minus;
  readonly TrashIcon = Trash2;
  readonly PackageIcon = Package;

  freeShippingThreshold = 50;

  amountAwayFromFreeShipping = computed(() => {
    const total = this.cartStore.totalPrice();
    return Math.max(0, this.freeShippingThreshold - total);
  });

  freeShippingPercentage = computed(() => {
    const total = this.cartStore.totalPrice();
    return Math.min(100, (total / this.freeShippingThreshold) * 100);
  });

  close() {
    this.cartStore.hideDrawer();
  }

  updateQuantity(productId: string, newQuantity: number) {
    this.cartStore.updateQuantity(productId, newQuantity);
  }

  removeItem(productId: string) {
    this.cartStore.removeFromCart(productId);
  }

  trackByCartItem(_index: number, item: { productId: string }): string {
    return item.productId;
  }
}
