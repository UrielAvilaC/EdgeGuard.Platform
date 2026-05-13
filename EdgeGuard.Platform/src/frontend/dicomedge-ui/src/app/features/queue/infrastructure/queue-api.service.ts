import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { DispatchStatus, QueueMessage, QueuedMessage, QueueSummary } from '../models/queue.models';

@Injectable({ providedIn: 'root' })
export class QueueApiService {
  private readonly api = inject(ApiClient);

  getSummary(): Observable<QueueSummary> {
    return this.api.get<QueueSummary>(API_ROUTES.QUEUE_MONITORING.SUMMARY);
  }

  getByStatus(status: DispatchStatus): Observable<QueueMessage[]> {
    return this.api.get<QueueMessage[]>(API_ROUTES.QUEUE_MONITORING.BY_STATUS(status));
  }

  getQueued(): Observable<QueuedMessage[]> {
    return this.api.get<QueuedMessage[]>(API_ROUTES.QUEUE_MONITORING.QUEUED);
  }
}
