import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { AuthStore } from '../store/auth.store';
import { API_ROUTES } from '../../api/api-routes';

let isRefreshing = false;

export const tokenRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const store = inject(AuthStore);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = req.url.includes(API_ROUTES.AUTH.LOGIN)
        || req.url.includes(API_ROUTES.AUTH.REFRESH);

      if (error.status !== 401 || isAuthEndpoint || !store.isAuthenticated()) {
        return throwError(() => error);
      }

      if (isRefreshing) {
        return throwError(() => error);
      }

      isRefreshing = true;

      return authService.refresh().pipe(
        switchMap((res) => {
          isRefreshing = false;
          const retryReq = req.clone({
            setHeaders: { Authorization: `Bearer ${res.accessToken}` },
          });
          return next(retryReq);
        }),
        catchError((refreshError) => {
          isRefreshing = false;
          authService.logout();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
