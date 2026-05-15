// Role-based route guard factory.
// Usage: canActivate: [roleGuard('Seller', 'Admin')]

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

export const roleGuard = (...roles: string[]): CanActivateFn => {
  return () => {
    const authStore = inject(AuthStore);
    const router = inject(Router);
    const user = authStore.user();

    if (user && roles.includes(user.role)) {
      return true;
    }

    return router.createUrlTree(['/']);
  };
};
