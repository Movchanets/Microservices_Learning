// Authentication route guard.
// Redirects unauthenticated users to the login page.

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

export const authGuard: CanActivateFn = () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (authStore.user() !== null) {
    return true;
  }

  return router.createUrlTree(['/auth/login']);
};
