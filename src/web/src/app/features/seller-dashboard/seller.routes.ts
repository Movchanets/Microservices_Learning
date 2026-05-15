// Seller dashboard routes.
// Lazy-loads seller dashboard components with nested routes for products and settings.

import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./dashboard-page/dashboard-page').then(c => c.SellerDashboardPageComponent),
    children: [
      {
        path: '',
        redirectTo: 'products',
        pathMatch: 'full',
      },
      {
        path: 'products',
        loadComponent: () => import('./product-list/product-list').then(c => c.SellerProductListComponent),
      },
      {
        path: 'products/new',
        loadComponent: () => import('./product-form/product-form').then(c => c.ProductFormComponent),
      },
      {
        path: 'products/:id/edit',
        loadComponent: () => import('./product-form/product-form').then(c => c.ProductFormComponent),
      },
      {
        path: 'orders',
        loadComponent: () => import('./seller-orders/seller-orders').then(c => c.SellerOrdersComponent),
      },
      {
        path: 'settings',
        loadComponent: () => import('./store-settings/store-settings').then(c => c.StoreSettingsComponent),
      },
    ],
  },
] as Routes;
