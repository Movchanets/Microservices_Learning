// Role-based route guard factory.
// Usage: canActivate: [roleGuard('Seller', 'Admin')]
// On SSR, always allows (client-side will handle redirect if needed).
// On client, waits for auth to complete before checking role.

import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

/** Wait for a signal predicate to become true, with timeout. */
function waitFor(predicate: () => boolean, timeoutMs = 5000, intervalMs = 50): Promise<boolean> {
  return new Promise(resolve => {
    if (predicate()) { resolve(true); return; }
    const start = Date.now();
    const timer = setInterval(() => {
      if (predicate()) { clearInterval(timer); resolve(true); }
      else if (Date.now() - start >= timeoutMs) { clearInterval(timer); resolve(false); }
    }, intervalMs);
  });
}

export const roleGuard = (...roles: string[]): CanActivateFn => {
  return async () => {
    const platformId = inject(PLATFORM_ID);

    // On SSR, allow navigation — client-side guard will redirect if wrong role
    if (!isPlatformBrowser(platformId)) {
      return true;
    }

    const authStore = inject(AuthStore);
    const router = inject(Router);

    // Wait for auth to finish loading
    await waitFor(() => !authStore.loading());

    const user = authStore.user();

    if (user && roles.includes(user.role)) {
      return true;
    }

    return router.createUrlTree(['/']);
  };
};
