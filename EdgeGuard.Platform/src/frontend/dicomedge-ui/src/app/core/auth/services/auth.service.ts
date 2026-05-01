import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { ApiClient } from '../../api/api-client';
import { API_ROUTES } from '../../api/api-routes';
import { AuthStore } from '../store/auth.store';
import {
  AuthResponse,
  LoginRequest,
  RefreshRequest,
  UserProfile,
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiClient);
  private readonly store = inject(AuthStore);
  private readonly router = inject(Router);

  init(): void {
    this.store.restoreFromStorage();
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.api.post<AuthResponse>(API_ROUTES.AUTH.LOGIN, request).pipe(
      tap((res) => {
        const user = this.decodeUserFromToken(res.accessToken, res.permissions);
        this.store.setAuth(res.accessToken, res.refreshToken, res.expiresIn, user);
      }),
    );
  }

  refresh(): Observable<AuthResponse> {
    const accessToken = this.store.accessToken();
    const refreshToken = this.store.refreshToken();

    if (!accessToken || !refreshToken) {
      this.logout();
      throw new Error('No tokens available for refresh');
    }

    const request: RefreshRequest = { accessToken, refreshToken };
    return this.api.post<AuthResponse>(API_ROUTES.AUTH.REFRESH, request).pipe(
      tap((res) => {
        const user = this.decodeUserFromToken(res.accessToken, res.permissions);
        this.store.setAuth(res.accessToken, res.refreshToken, res.expiresIn, user);
      }),
    );
  }

  me(): Observable<UserProfile> {
    return this.api.get<UserProfile>(API_ROUTES.AUTH.ME).pipe(
      tap((user) => this.store.setUser(user)),
    );
  }

  logout(): void {
    const refreshToken = this.store.refreshToken();
    if (refreshToken) {
      this.api.post(API_ROUTES.AUTH.REVOKE, { refreshToken }).subscribe();
    }
    this.store.clearAuth();
    this.router.navigate(['/login']);
  }

  revokeAll(): Observable<void> {
    return this.api.post<void>(API_ROUTES.AUTH.REVOKE_ALL).pipe(
      tap(() => {
        this.store.clearAuth();
        this.router.navigate(['/login']);
      }),
    );
  }

  private decodeUserFromToken(token: string, permissions?: string[]): UserProfile {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        userId: payload.userId ?? payload.sub ?? '',
        userName: payload.userName ?? payload.unique_name ?? '',
        fullName: payload.fullName ?? payload.name ?? '',
        roles: Array.isArray(payload.role) ? payload.role : payload.role ? [payload.role] : [],
        permissions: permissions ?? [],
      };
    } catch {
      return { userId: '', userName: '', fullName: '', roles: [], permissions: [] };
    }
  }
}
