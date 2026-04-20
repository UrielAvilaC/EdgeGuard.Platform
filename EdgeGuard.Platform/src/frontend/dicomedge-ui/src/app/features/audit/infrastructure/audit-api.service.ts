import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import { AuditLogDto, AuditLogFilter } from '../models/audit.models';

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getPaged(filter: AuditLogFilter): Observable<PagedResult<AuditLogDto>> {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDir) params = params.set('sortDir', filter.sortDir);
    if (filter.eventType) params = params.set('eventType', filter.eventType);
    if (filter.severity) params = params.set('severity', filter.severity);
    if (filter.userId) params = params.set('userId', filter.userId);
    if (filter.entityType) params = params.set('entityType', filter.entityType);
    if (filter.entityId) params = params.set('entityId', filter.entityId);
    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom);
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo);
    if (filter.isSuccess !== undefined) params = params.set('isSuccess', filter.isSuccess);
    if (filter.search) params = params.set('search', filter.search);
    return this.http.get<PagedResult<AuditLogDto>>(`${this.base}${API_ROUTES.AUDIT_LOGS.LIST}`, { params });
  }

  getByEntity(entityType: string, entityId: string): Observable<AuditLogDto[]> {
    return this.http.get<AuditLogDto[]>(`${this.base}${API_ROUTES.AUDIT_LOGS.BY_ENTITY(entityType, entityId)}`);
  }

  getByCorrelation(correlationId: string): Observable<AuditLogDto[]> {
    return this.http.get<AuditLogDto[]>(`${this.base}${API_ROUTES.AUDIT_LOGS.BY_CORRELATION(correlationId)}`);
  }

  getEventTypes(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}${API_ROUTES.AUDIT_LOGS.EVENT_TYPES}`);
  }
}
