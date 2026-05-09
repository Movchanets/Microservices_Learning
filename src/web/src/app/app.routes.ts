import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then(m => m.Login)
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register').then(m => m.Register)
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./features/auth/forgot-password/forgot-password').then(m => m.ForgotPassword)
      }
    ]
  },
  {
    path: 'catalog',
    loadComponent: () => import('./features/catalog/catalog').then(m => m.CatalogComponent)
  }
];
