import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { ToastService } from '../services/toast.service';
import { isApiError } from '../api/api-error.model';
import { API_ROUTES } from '../api/api-routes';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
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
