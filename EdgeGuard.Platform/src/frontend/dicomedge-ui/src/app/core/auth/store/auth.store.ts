import { Injectable, computed, signal } from '@angular/core';

import { UserProfile, StoredAuth } from '../models/auth.models';

const STORAGE_KEY = 'edgeguard_auth';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _accessToken = signal<string | null>(null);
  private readonly _refreshToken = signal<string | null>(null);
  private readonly _expiresAt = signal<number | null>(null);
  private readonly _user = signal<UserProfile | null>(null);

  readonly accessToken = this._accessToken.asReadonly();
  readonly refreshToken = this._refreshToken.asReadonly();
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);
  readonly permissions = computed(() => this._user()?.permissions ?? []);
  readonly roles = computed(() => this._user()?.roles ?? []);
  readonly isTokenExpired = computed(() => {
    const exp = this._expiresAt();
    return exp !== null && Date.now() >= exp;
  });

  setAuth(accessToken: string, refreshToken: string, expiresIn: number, user: UserProfile): void {
    const expiresAt = Date.now() + expiresIn * 1000;
    this._accessToken.set(accessToken);
    this._refreshToken.set(refreshToken);
    this._expiresAt.set(expiresAt);
    this._user.set(user);
    this.persist({ accessToken, refreshToken, expiresAt, user });
  }

  updateTokens(accessToken: string, refreshToken: string, expiresIn: number): void {
    const expiresAt = Date.now() + expiresIn * 1000;
    this._accessToken.set(accessToken);
    this._refreshToken.set(refreshToken);
    this._expiresAt.set(expiresAt);
    const user = this._user();
    if (user) {
      this.persist({ accessToken, refreshToken, expiresAt, user });
    }
  }

  setUser(user: UserProfile): void {
    this._user.set(user);
  }

  clearAuth(): void {
    this._accessToken.set(null);
    this._refreshToken.set(null);
    this._expiresAt.set(null);
    this._user.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  restoreFromStorage(): boolean {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return false;

      const stored: StoredAuth = JSON.parse(raw);
      if (Date.now() >= stored.expiresAt) {
        localStorage.removeItem(STORAGE_KEY);
        return false;
      }

      this._accessToken.set(stored.accessToken);
      this._refreshToken.set(stored.refreshToken);
      this._expiresAt.set(stored.expiresAt);
      this._user.set(stored.user);
      return true;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return false;
    }
  }

  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  private persist(auth: StoredAuth): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
  }
}
