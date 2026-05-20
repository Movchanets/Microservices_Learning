import { Routes } from '@angular/router';

export const STORE_ROUTES: Routes = [
  {
    path: ':id',
    loadComponent: () => import('./store-page/store-page').then((m) => m.StorePageComponent),
  },
];
