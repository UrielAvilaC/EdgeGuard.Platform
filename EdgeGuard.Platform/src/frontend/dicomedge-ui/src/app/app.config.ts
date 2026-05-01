import { ApplicationConfig, provideZonelessChangeDetection, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';

import { routes } from './app.routes';
import { ENVIRONMENT } from './core/config/environment.config';
import { environment } from '../environments/environment';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { correlationIdInterceptor } from './core/interceptors/correlation-id.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { authInterceptor } from './core/auth/interceptors/auth.interceptor';
import { tokenRefreshInterceptor } from './core/auth/interceptors/token-refresh.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions({ skipInitialTransition: true })),
    provideHttpClient(
      withInterceptors([
        correlationIdInterceptor,
        authInterceptor,
        retryInterceptor,
        loadingInterceptor,
        tokenRefreshInterceptor,
        errorInterceptor,
      ]),
    ),
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    { provide: ENVIRONMENT, useValue: environment },
  ],
};
