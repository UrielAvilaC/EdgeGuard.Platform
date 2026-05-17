import { HttpInterceptorFn } from '@angular/common/http';

function generateCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  // Fallback for non-secure contexts (HTTP)
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
}

export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationId = generateCorrelationId();

  const cloned = req.clone({
    setHeaders: { 'X-Correlation-Id': correlationId },
  });

  return next(cloned);
};
