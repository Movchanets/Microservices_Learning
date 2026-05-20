import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
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
  {
    path: 'checkout',
    renderMode: RenderMode.Server,
  },
  {
    path: 'orders',
    renderMode: RenderMode.Server,
  },
  {
    path: 'orders/:id',
    renderMode: RenderMode.Server,
  },
  {
    path: 'seller',
    renderMode: RenderMode.Server,
  },
  {
    path: 'seller/**',
    renderMode: RenderMode.Server,
  },
  {
    path: 'admin',
    renderMode: RenderMode.Server,
  },
  {
    path: 'admin/**',
    renderMode: RenderMode.Server,
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender,
  },
];
