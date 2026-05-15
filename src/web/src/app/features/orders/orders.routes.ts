import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./order-list/order-list').then((c) => c.OrderListComponent),
  },
  {
    path: ':id',
    loadComponent: () => import('./order-detail/order-detail').then((c) => c.OrderDetailComponent),
  },
] as Routes;
