import { HttpInterceptorFn } from '@angular/common/http';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  let url = req.url;
  if (!url.startsWith('http') && !url.startsWith('/api') && !url.startsWith('/bff')) {
    url = `/api${url.startsWith('/') ? '' : '/'}${url}`;
  }

  const apiReq = req.clone({
    url,
    withCredentials: true,
  });
  return next(apiReq);
};
