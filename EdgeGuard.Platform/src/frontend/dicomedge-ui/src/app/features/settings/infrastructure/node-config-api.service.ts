import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { API_ROUTES } from '../../../core/api/api-routes';
import {
  NodeConfigurationProfileDto,
  ConfigPushResultDto,
  UpdateSettingRequest,
  BatchUpdateNodeSettingsRequest,
  BatchUpdateNodeSettingsResponse,
} from '../models/settings.models';

@Injectable({ providedIn: 'root' })
export class NodeConfigApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getByNode(nodeId: string): Observable<NodeConfigurationProfileDto[]> {
    return this.http.get<NodeConfigurationProfileDto[]>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.BY_NODE(nodeId)}`);
  }

  getByCategory(nodeId: string, category: string): Observable<NodeConfigurationProfileDto[]> {
    return this.http.get<NodeConfigurationProfileDto[]>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.BY_CATEGORY(nodeId, category)}`);
  }

  updateSetting(nodeId: string, key: string, request: UpdateSettingRequest): Observable<void> {
    return this.http.put<void>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.UPDATE(nodeId, key)}`, request);
  }

  batchUpdate(nodeId: string, request: BatchUpdateNodeSettingsRequest): Observable<BatchUpdateNodeSettingsResponse> {
    return this.http.put<BatchUpdateNodeSettingsResponse>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.BATCH_UPDATE(nodeId)}`, request);
  }

  resetSetting(nodeId: string, key: string): Observable<void> {
    return this.http.post<void>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.RESET(nodeId, key)}`, {});
  }

  resetCategory(nodeId: string, category: string): Observable<{ resetCount: number }> {
    return this.http.post<{ resetCount: number }>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.RESET_CATEGORY(nodeId, category)}`, {});
  }

  pushConfig(nodeId: string): Observable<ConfigPushResultDto> {
    return this.http.post<ConfigPushResultDto>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.PUSH(nodeId)}`, {});
  }

  getVersion(nodeId: string): Observable<{ configVersion: string }> {
    return this.http.get<{ configVersion: string }>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.VERSION(nodeId)}`);
  }
}
