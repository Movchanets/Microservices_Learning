// Authentication route guard.
// Skips on SSR (allows navigation), checks on client after auth loads.

import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

export const authGuard: CanActivateFn = async () => {
  const platformId = inject(PLATFORM_ID);

  // Skip on SSR — client will handle redirect
  if (!isPlatformBrowser(platformId)) {
    return true;
  }

  const authStore = inject(AuthStore);
  const router = inject(Router);

  // Wait for auth to load (app initializer awaits checkAuth)
  if (authStore.loading()) {
    for (let i = 0; i < 100; i++) {
      await new Promise(r => setTimeout(r, 100));
      if (!authStore.loading()) break;
    }
  }

  if (authStore.user() !== null) {
    return true;
  }

  return router.createUrlTree(['/auth/login']);
};
