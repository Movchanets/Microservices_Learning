import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'catalog',
    pathMatch: 'full',
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/auth/profile/profile').then((m) => m.ProfileComponent),
  },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
      },
      {
        path: 'forgot-password',
        loadComponent: () =>
          import('./features/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
      },
    ],
  },
  {
    path: 'catalog',
    loadChildren: () => import('./features/catalog/catalog.routes').then((m) => m.CATALOG_ROUTES),
  },
  {
    path: 'cart',
    canActivate: [authGuard],
    loadChildren: () => import('./features/cart/cart.routes'),
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadChildren: () => import('./features/checkout/checkout.routes'),
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadChildren: () => import('./features/orders/orders.routes'),
  },
  {
    path: 'seller',
    canActivate: [authGuard, roleGuard('Seller', 'Admin')],
    loadChildren: () => import('./features/seller-dashboard/seller.routes'),
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard('Admin')],
    loadChildren: () => import('./features/admin/admin.routes'),
  },
  {
    path: '**',
    loadComponent: () => import('./shared/pages/not-found/not-found').then(m => m.NotFoundComponent),
  },
];
