// Admin panel routes.
// Lazy-loads admin components with nested routes for users, verifications, and stores.

import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./admin-page/admin-page').then(c => c.AdminPageComponent),
    children: [
      {
        path: '',
        redirectTo: 'users',
        pathMatch: 'full',
      },
      {
        path: 'users',
        loadComponent: () => import('./user-list/user-list').then(c => c.UserListComponent),
      },
      {
        path: 'verifications',
        loadComponent: () => import('./store-verification/store-verification').then(c => c.StoreVerificationComponent),
      },
      {
        path: 'stores',
        loadComponent: () => import('./store-verification/store-verification').then(c => c.StoreVerificationComponent),
      },
      {
        path: 'stores/:id',
        loadComponent: () => import('./store-detail/store-detail').then(c => c.StoreDetailComponent),
      },
      {
        path: 'attributes',
        loadComponent: () => import('./category-attributes/category-attributes').then(c => c.CategoryAttributesComponent),
      },
    ],
  },
] as Routes;
