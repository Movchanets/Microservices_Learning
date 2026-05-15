import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError(err => {
      if (err.status === 0 || err.status >= 500) {
        const message = err.error?.message || err.statusText || 'An error occurred';
        toast.error(message);
      }
      return throwError(() => err);
    })
  );
};
