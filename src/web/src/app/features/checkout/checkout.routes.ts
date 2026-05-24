import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./checkout-page/checkout-page').then((c) => c.CheckoutPageComponent),
  },
] as Routes;
