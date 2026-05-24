import { Routes } from '@angular/router';

export const PROFILE_ROUTES: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  { 
    path: 'orders', 
    loadComponent: () => import('../../orders/order-list/order-list').then(m => m.OrderListComponent) 
  },
  { 
    path: 'settings', 
    loadComponent: () => import('./components/profile-settings/profile-settings').then(m => m.ProfileSettingsComponent) 
  },
];