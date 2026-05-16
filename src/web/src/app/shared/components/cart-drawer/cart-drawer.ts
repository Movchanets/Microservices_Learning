import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, X, Plus, Minus, Trash2 } from 'lucide-angular';
import { CartStore } from '../../../features/cart/cart.store';

@Component({
  selector: 'app-cart-drawer',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule],
  templateUrl: './cart-drawer.html',
  styleUrl: './cart-drawer.css',
})
export class CartDrawer {
  cartStore = inject(CartStore);

  readonly XIcon = X;
  readonly PlusIcon = Plus;
  readonly MinusIcon = Minus;
  readonly TrashIcon = Trash2;

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

  updateQuantity(sku: string, newQuantity: number) {
    this.cartStore.updateQuantity(sku, newQuantity);
  }

  removeItem(sku: string) {
    this.cartStore.removeFromCart(sku);
  }
}
