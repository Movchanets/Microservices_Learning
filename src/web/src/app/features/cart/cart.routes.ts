import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./cart-page/cart-page').then(c => c.CartPageComponent)
  }
] as Routes;