import { Routes } from '@angular/router';

export const CATALOG_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./product-list/product-list').then(m => m.ProductListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./product-detail/product-detail').then(m => m.ProductDetailComponent),
  },
];
