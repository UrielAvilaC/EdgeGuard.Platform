import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { API_ROUTES } from '../../../core/api/api-routes';
import { SystemSettingDto, UpdateSettingRequest } from '../models/settings.models';

@Injectable({ providedIn: 'root' })
export class SystemSettingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getAll(): Observable<SystemSettingDto[]> {
    return this.http.get<SystemSettingDto[]>(`${this.base}${API_ROUTES.SYSTEM_SETTINGS.LIST}`);
  }

  getByCategory(category: string): Observable<SystemSettingDto[]> {
    return this.http.get<SystemSettingDto[]>(`${this.base}${API_ROUTES.SYSTEM_SETTINGS.BY_CATEGORY(category)}`);
  }

  getByKey(key: string): Observable<SystemSettingDto> {
    return this.http.get<SystemSettingDto>(`${this.base}${API_ROUTES.SYSTEM_SETTINGS.BY_KEY(key)}`);
  }

  update(key: string, request: UpdateSettingRequest): Observable<void> {
    return this.http.put<void>(`${this.base}${API_ROUTES.SYSTEM_SETTINGS.BY_KEY(key)}`, request);
  }

  seedDefaults(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}${API_ROUTES.SYSTEM_SETTINGS.SEED_DEFAULTS}`, {});
  }
}
