import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * Estado de aplicación de la configuración de un nodo.
 *
 * Son dos versiones y no un booleano de éxito porque aplicar configuración no es
 * un evento sino un estado: el push inmediato puede fallar y el nodo recibirla
 * igual por el reintento del outbox o por su propio pull.
 */
export interface NodeConfigVersionState {
  /** La que el Hub quiere que tenga. */
  configVersion: string;
  /** La que el nodo confirmó tener. null = nunca confirmó. */
  appliedVersion: string | null;
  appliedAt: string | null;
  isApplied: boolean;
}

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

  getVersion(nodeId: string): Observable<NodeConfigVersionState> {
    return this.http.get<NodeConfigVersionState>(`${this.base}${API_ROUTES.NODE_CONFIGURATION.VERSION(nodeId)}`);
  }
}
