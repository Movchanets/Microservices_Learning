import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'catalog',
    renderMode: RenderMode.Server,
  },
  {
    path: 'catalog/:id',
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
    path: '**',
    renderMode: RenderMode.Prerender,
  },
];
