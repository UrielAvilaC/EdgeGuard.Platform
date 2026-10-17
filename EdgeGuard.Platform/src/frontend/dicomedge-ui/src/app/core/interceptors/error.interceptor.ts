import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { ToastService } from '../services/toast.service';
import { isApiError } from '../api/api-error.model';
import { API_ROUTES } from '../api/api-routes';
import { SKIP_ERROR_TOAST } from '../api/http-context.tokens';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);

  // Peticiones cuyo fallo es parte del flujo normal: quien las lanza explica el
  // resultado con sus propias palabras y el toast genérico sólo lo contradice.
  // La redirección por 401 se mantiene: eso no es un mensaje, es sesión caducada.
  const silenciar = req.context.get(SKIP_ERROR_TOAST);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (silenciar && error.status !== 401) {
        return throwError(() => error);
      }

      if (error.status === 0) {
        toast.error('No se puede conectar con el servidor. Verifique su conexión.');
        return throwError(() => error);
      }

      // Auth endpoints handle their own 401 responses (wrong credentials, expired token).
      // Only redirect to /login for 401s from protected endpoints.
      const isAuthEndpoint = req.url.includes(API_ROUTES.AUTH.LOGIN)
        || req.url.includes(API_ROUTES.AUTH.REFRESH);

      if (error.status === 401 && !isAuthEndpoint) {
        router.navigate(['/login']);
        return throwError(() => error);
      }

      if (error.status === 403) {
        toast.warning('No tiene permisos para realizar esta acción.');
        return throwError(() => error);
      }

      if (error.status === 404) {
        return throwError(() => error);
      }

      if (error.status === 422 && isApiError(error.error)) {
        return throwError(() => error);
      }

      if (error.status >= 500) {
        toast.error('Error interno del servidor. Intente nuevamente.');
        return throwError(() => error);
      }

      const detail = error.error?.detail ?? error.error?.title ?? 'Ocurrió un error inesperado.';
      toast.error(detail);
      return throwError(() => error);
    }),
  );
};
