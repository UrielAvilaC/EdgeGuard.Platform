import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import { Study, StudyFilter, UpdateStudyRequest, UpdateStudyStatusRequest } from '../models/study.models';

@Injectable({ providedIn: 'root' })
export class StudiesApiService {
  private readonly api = inject(ApiClient);

  getStudies(filter: StudyFilter): Observable<PagedResult<Study>> {
    return this.api.get<PagedResult<Study>>(API_ROUTES.STUDIES.LIST, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }

  getById(id: string): Observable<Study> {
    return this.api.get<Study>(API_ROUTES.STUDIES.BY_ID(id));
  }

  getByUid(uid: string): Observable<Study> {
    return this.api.get<Study>(API_ROUTES.STUDIES.BY_UID(uid));
  }

  getByPatient(patientId: string): Observable<Study[]> {
    return this.api.get<Study[]>(API_ROUTES.STUDIES.BY_PATIENT(patientId));
  }

  getByNode(nodeId: string): Observable<Study[]> {
    return this.api.get<Study[]>(API_ROUTES.STUDIES.BY_NODE(nodeId));
  }

  getPendingPacs(): Observable<Study[]> {
    return this.api.get<Study[]>(API_ROUTES.STUDIES.PENDING_PACS);
  }

  getCount(): Observable<{ count: number }> {
    return this.api.get<{ count: number }>(API_ROUTES.STUDIES.COUNT);
  }

  update(id: string, request: UpdateStudyRequest): Observable<Study> {
    return this.api.put<Study>(API_ROUTES.STUDIES.BY_ID(id), request);
  }

  updateStatus(id: string, request: UpdateStudyStatusRequest): Observable<Study> {
    return this.api.put<Study>(API_ROUTES.STUDIES.STATUS(id), request);
  }

  export(filter: StudyFilter): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.api.getBlob(API_ROUTES.STUDIES.EXPORT, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }
}
