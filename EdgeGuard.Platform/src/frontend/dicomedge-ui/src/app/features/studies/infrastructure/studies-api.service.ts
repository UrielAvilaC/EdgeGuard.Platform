import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import { Study, StudyFilter, StudyInfrastructure, StudyReport, UpdateStudyRequest, UpdateStudyStatusRequest } from '../models/study.models';

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

  requeue(id: string, pacsIds: string[]): Observable<Study> {
    return this.api.post<Study>(API_ROUTES.STUDIES.REQUEUE(id), { pacsIds });
  }

  getInfrastructure(id: string): Observable<StudyInfrastructure> {
    return this.api.get<StudyInfrastructure>(API_ROUTES.STUDIES.INFRASTRUCTURE(id));
  }

  export(filter: StudyFilter): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.api.getBlob(API_ROUTES.STUDIES.EXPORT, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }

  getReport(id: string): Observable<StudyReport> {
    return this.api.get<StudyReport>(API_ROUTES.STUDIES.REPORT(id));
  }

  getReportPdf(id: string): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.api.getBlob(API_ROUTES.STUDIES.REPORT_PDF(id));
  }

  getReportQr(id: string): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.api.getBlob(API_ROUTES.STUDIES.REPORT_QR(id));
  }

  getEmailTemplates(): Observable<{ id: string; name: string }[]> {
    return this.api.get<{ id: string; name: string }[]>(API_ROUTES.NOTIFICATION_TEMPLATES.LIST);
  }

  getWhatsAppTemplates(): Observable<{ id: string; name: string }[]> {
    return this.api.get<{ id: string; name: string }[]>(API_ROUTES.WHATSAPP.TEMPLATES);
  }

  deliver(id: string, body: DeliverResultsRequest): Observable<{ enqueued: number }> {
    return this.api.post<{ enqueued: number }>(`/studies/${id}/deliver`, body);
  }

  getDeliveries(id: string): Observable<DeliveryHistory[]> {
    return this.api.get<DeliveryHistory[]>(`/studies/${id}/deliveries`);
  }
}

export interface DeliverResultsRequest {
  emails: string[];
  phones: string[];
  attachPdf: boolean;
  includeQr: boolean;
  emailTemplateId?: string | null;
  whatsAppTemplateId?: string | null;
}

export interface DeliveryHistory {
  id: string;
  channel: string;
  to: string;
  status: string;
  sentAt: string | null;
  error: string | null;
  createdAt: string;
}
