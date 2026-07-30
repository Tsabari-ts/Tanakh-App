import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { retry, timer } from 'rxjs';

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  // reads only — never POST/PUT/DELETE (risk of duplicate side effects)
  if (req.method !== 'GET') return next(req);

  return next(req).pipe(
    retry({
      count: 2,
      delay: (error: HttpErrorResponse, retryCount) => {
        if (error.status >= 400 && error.status < 500) throw error; // retrying is pointless
        return timer(Math.pow(2, retryCount) * 500); // exponential backoff
      },
    }),
  );
};
