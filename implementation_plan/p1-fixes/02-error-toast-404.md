# P1-02 — Error Toast + 404 Page

**Goal**: Add global HTTP error handling with toast notifications and a 404 page.

**Fixes**: MISSING.md #5.4, #5.5

---

## Error Toast Service

File: `src/web/src/app/core/services/toast.service.ts`
```typescript
import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private counter = 0;
  readonly toasts = signal<Toast[]>([]);

  show(message: string, type: Toast['type'] = 'info', duration = 5000): void {
    const id = ++this.counter;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.dismiss(id), duration);
  }

  error(message: string): void { this.show(message, 'error'); }
  success(message: string): void { this.show(message, 'success'); }

  dismiss(id: number): void {
    this.toasts.update(t => t.filter(x => x.id !== id));
  }
}
```

## HTTP Error Interceptor

File: `src/web/src/app/core/http/error.interceptor.ts`
```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError(err => {
      const message = err.error?.message || err.statusText || 'An error occurred';
      toast.error(message);
      return throwError(() => err);
    })
  );
};
```

Register in `app.config.ts`:
```typescript
provideHttpClient(withInterceptors([apiInterceptor, errorInterceptor]))
```

## Toast Container Component

File: `src/web/src/app/shared/components/toast-container/toast-container.ts`

Render toast stack in bottom-right corner with dismiss buttons. Add to `app.ts` template.

## 404 Page

File: `src/web/src/app/shared/pages/not-found/not-found.ts`
```typescript
@Component({
  template: `
    <div class="text-center py-20">
      <h1 class="text-6xl font-bold text-foreground mb-4">404</h1>
      <p class="text-xl text-muted mb-8">Page not found</p>
      <a routerLink="/" class="px-6 py-3 rounded-xl bg-primary text-white">Go Home</a>
    </div>
  `
})
export class NotFoundComponent {}
```

Add catch-all route at the end of `app.routes.ts`:
```typescript
{ path: '**', component: NotFoundComponent }
```

## Done When
- [ ] ToastService with show/dismiss
- [ ] ErrorInterceptor registered
- [ ] Toast container in app template
- [ ] 404 page with catch-all route
