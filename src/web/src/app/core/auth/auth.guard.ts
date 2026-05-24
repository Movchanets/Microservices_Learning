// Authentication route guard.
// Skips on SSR (allows navigation), checks on client after auth loads.

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

export const authGuard: CanActivateFn = async () => {
  const platformId = inject(PLATFORM_ID);

  // Skip on SSR — client will handle redirect
  if (!isPlatformBrowser(platformId)) {
    return true;
  }

  const authStore = inject(AuthStore);
  const router = inject(Router);

  // Wait for auth to finish loading (app initializer awaits checkAuth)
  await waitFor(() => !authStore.loading());

  if (authStore.user() !== null) {
    return true;
  }

  return router.createUrlTree(['/auth/login']);
};
