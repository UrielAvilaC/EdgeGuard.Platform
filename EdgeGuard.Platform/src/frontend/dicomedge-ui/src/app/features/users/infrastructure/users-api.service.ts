import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import { UserDto, UserFilter, CreateUserRequest, UpdateProfileRequest, ResetPasswordRequest } from '../models/users.models';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getUsers(filter: UserFilter): Observable<PagedResult<UserDto>> {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDir) params = params.set('sortDir', filter.sortDir);
    if (filter.search) params = params.set('search', filter.search);
    if (filter.isActive !== undefined) params = params.set('isActive', filter.isActive);
    if (filter.role) params = params.set('role', filter.role);
    return this.http.get<PagedResult<UserDto>>(`${this.base}${API_ROUTES.USERS.LIST}`, { params });
  }

  getById(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.base}${API_ROUTES.USERS.BY_ID(id)}`);
  }

  create(request: CreateUserRequest): Observable<UserDto> {
    return this.http.post<UserDto>(`${this.base}${API_ROUTES.USERS.LIST}`, request);
  }

  updateProfile(id: string, request: UpdateProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.base}${API_ROUTES.USERS.PROFILE(id)}`, request);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ROUTES.USERS.DEACTIVATE(id)}`, {});
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ROUTES.USERS.ACTIVATE(id)}`, {});
  }

  assignRole(id: string, role: string): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ROUTES.USERS.ROLES(id)}`, { role });
  }

  removeRole(id: string, role: string): Observable<void> {
    return this.http.delete<void>(`${this.base}${API_ROUTES.USERS.REMOVE_ROLE(id, role)}`);
  }

  grantPermission(id: string, permission: string): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ROUTES.USERS.PERMISSIONS(id)}`, { permission });
  }

  revokePermission(id: string, permission: string): Observable<void> {
    return this.http.delete<void>(`${this.base}${API_ROUTES.USERS.REMOVE_PERMISSION(id, permission)}`);
  }

  resetPassword(id: string, request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ROUTES.USERS.RESET_PASSWORD(id)}`, request);
  }

  unlock(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ROUTES.USERS.UNLOCK(id)}`, {});
  }
}
