import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap, finalize } from 'rxjs';

import { ENVIRONMENT } from '../config/environment.config';
import { API_ROUTES } from '../api/api-routes';

const TAG = '[HTTP Diagnostic]';

export const diagnosticInterceptor: HttpInterceptorFn = (req, next) => {
  const env = inject(ENVIRONMENT);

  // Only log login and refresh to avoid noise
  const trackedPaths = [API_ROUTES.AUTH.LOGIN, API_ROUTES.AUTH.REFRESH];
  const isTracked = trackedPaths.some((p) => req.url.includes(p));

  if (!isTracked) {
    return next(req);
  }

  const t0 = performance.now();

  console.group(`${TAG} ${req.method} ${req.url}`);
  console.log('Environment:', env.production ? 'production' : 'development');
  console.log('Full URL:', req.url);
  console.log('apiBaseUrl:', env.apiBaseUrl);
  console.log('Origin (window.location):', window.location.origin);
  console.log('Request headers:', Object.fromEntries(
    req.headers.keys().map((k) => [k, req.headers.get(k)])
  ));
  console.groupEnd();

  return next(req).pipe(
    tap({
      next: (event) => {
        const ms = Math.round(performance.now() - t0);
        console.log(`${TAG} ✅ Success (${ms}ms)`, event);
      },
      error: (error) => {
        const ms = Math.round(performance.now() - t0);
        console.group(`${TAG} ❌ Error (${ms}ms)`);
        console.error('Status:', error.status);
        console.error('Status text:', error.statusText);
        console.error('URL:', error.url);
        console.error('Error body:', error.error);

        if (error.status === 0) {
          console.warn(
            '⚠️  Status 0 = red de redes bloqueó la petición antes de llegar al servidor.\n' +
            '   Causas comunes: CORS preflight rechazado, firewall, o la URL no responde.\n' +
            `   URL intentada: ${req.url}\n` +
            `   Origen del browser: ${window.location.origin}`
          );
        }
        console.groupEnd();
      },
    }),
    finalize(() => {
      const ms = Math.round(performance.now() - t0);
      console.log(`${TAG} 🏁 Finalized (${ms}ms) ${req.method} ${req.url}`);
    }),
  );
};
