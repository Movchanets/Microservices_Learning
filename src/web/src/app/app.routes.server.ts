import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // ── Public routes — SSR for SEO and fast first paint ──────
  {
    path: 'home',
    renderMode: RenderMode.Server,
  },
  {
    path: 'catalog',
    renderMode: RenderMode.Server,
  },
  {
    path: 'catalog/:id',
    renderMode: RenderMode.Server,
  },
  {
    path: 'stores',
    renderMode: RenderMode.Server,
  },
  {
    path: 'stores/:id',
    renderMode: RenderMode.Server,
  },

  // ── Authenticated routes — client-only (SSR can't impersonate users) ──
  {
    path: 'profile',
    renderMode: RenderMode.Client,
  },
  {
    path: 'profile/**',
    renderMode: RenderMode.Client,
  },
  {
    path: 'cart',
    renderMode: RenderMode.Client,
  },
  {
    path: 'checkout',
    renderMode: RenderMode.Client,
  },
  {
    path: 'orders',
    renderMode: RenderMode.Client,
  },
  {
    path: 'orders/:id',
    renderMode: RenderMode.Client,
  },
  {
    path: 'seller',
    renderMode: RenderMode.Client,
  },
  {
    path: 'seller/**',
    renderMode: RenderMode.Client,
  },
  {
    path: 'admin',
    renderMode: RenderMode.Client,
  },
  {
    path: 'admin/**',
    renderMode: RenderMode.Client,
  },

  // ── Catch-all — prerender static pages ────────────────────
  {
    path: '**',
    renderMode: RenderMode.Prerender,
  },
];
