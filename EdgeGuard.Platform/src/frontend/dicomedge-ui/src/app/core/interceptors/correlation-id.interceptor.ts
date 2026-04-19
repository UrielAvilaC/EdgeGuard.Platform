import { HttpInterceptorFn } from '@angular/common/http';

export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationId = crypto.randomUUID();

  const cloned = req.clone({
    setHeaders: { 'X-Correlation-Id': correlationId },
  });

  return next(cloned);
};
