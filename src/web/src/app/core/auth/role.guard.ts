// Role-based route guard factory.
// Usage: canActivate: [roleGuard('Seller', 'Admin')]
// On SSR, always allows (client-side will handle redirect if needed).
// On client, waits for auth to complete before checking role.

import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

export const roleGuard = (...roles: string[]): CanActivateFn => {
  return async () => {
    const platformId = inject(PLATFORM_ID);

    // On SSR, allow navigation — client-side guard will redirect if wrong role
    if (!isPlatformBrowser(platformId)) {
      return true;
    }

    const authStore = inject(AuthStore);
    const router = inject(Router);

    // Wait for auth to load if still loading
    if (authStore.loading()) {
      for (let i = 0; i < 100; i++) {
        await new Promise(r => setTimeout(r, 100));
        if (!authStore.loading()) break;
      }
    }

    const user = authStore.user();

    if (user && roles.includes(user.role)) {
      return true;
    }

    return router.createUrlTree(['/']);
  };
};
