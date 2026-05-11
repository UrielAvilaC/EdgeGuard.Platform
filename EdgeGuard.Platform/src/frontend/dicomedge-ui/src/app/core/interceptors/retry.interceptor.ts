import { HttpInterceptorFn } from '@angular/common/http';
import { retry, timer } from 'rxjs';

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  // Only retry idempotent GET requests on network/server errors
  if (req.method !== 'GET') {
    return next(req);
  }

  return next(req).pipe(
    retry({
      count: 2,
      delay: (error, retryCount) => {
        // Only retry on network errors (status 0) or server errors (5xx)
        if (error.status === 0 || error.status >= 500) {
          return timer(retryCount * 1000); // exponential-ish backoff: 1s, 2s
        }
        throw error;
      },
    }),
  );
};
