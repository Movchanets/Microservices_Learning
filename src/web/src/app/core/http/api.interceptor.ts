import { HttpInterceptorFn } from '@angular/common/http';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  let url = req.url;
  // Prepend base URL if it's a relative path and not already pointing to a full URL or /api
  // In our case, the backend gateway is at /api
  if (url.startsWith('/') && !url.startsWith('/api')) {
    url = `/api${url}`;
  }

  const apiReq = req.clone({
    url,
    withCredentials: true,
  });
  return next(apiReq);
};
