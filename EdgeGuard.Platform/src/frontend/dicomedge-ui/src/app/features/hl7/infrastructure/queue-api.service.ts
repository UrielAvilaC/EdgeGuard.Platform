import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { Hl7DispatchStatus, Hl7Message, Hl7MessageQueued, QueueSummary } from '../models/hl7.models';

@Injectable({ providedIn: 'root' })
export class QueueApiService {
  private readonly api = inject(ApiClient);

  getSummary(): Observable<QueueSummary> {
    return this.api.get<QueueSummary>(API_ROUTES.QUEUE_MONITORING.SUMMARY);
  }

  getByDispatchStatus(status: Hl7DispatchStatus): Observable<Hl7Message[]> {
    return this.api.get<Hl7Message[]>(API_ROUTES.QUEUE_MONITORING.BY_STATUS(status));
  }

  getQueued(batchSize = 50): Observable<Hl7MessageQueued[]> {
    return this.api.get<Hl7MessageQueued[]>(API_ROUTES.QUEUE_MONITORING.QUEUED, {
      params: { batchSize } as Record<string, string | number | boolean | undefined>,
    });
  }
}
