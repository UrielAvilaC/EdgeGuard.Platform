import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { OutboxActivity, OutboxTopic, PagedResult } from '../models/outbox.models';

@Injectable({ providedIn: 'root' })
export class OutboxApiService {
  private readonly api = inject(ApiClient);

  getActivity(params: {
    category?: string;
    topic?: string;
    status?: string;
    page: number;
    pageSize: number;
  }): Observable<PagedResult<OutboxActivity>> {
    return this.api.get<PagedResult<OutboxActivity>>(API_ROUTES.OUTBOX.ACTIVITY, { params });
  }

  getTopics(): Observable<OutboxTopic[]> {
    return this.api.get<OutboxTopic[]>(API_ROUTES.OUTBOX.TOPICS);
  }

  retry(store: string, id: string): Observable<void> {
    return this.api.post<void>(API_ROUTES.OUTBOX.RETRY(store, id));
  }

  deadLetter(store: string, id: string): Observable<void> {
    return this.api.post<void>(API_ROUTES.OUTBOX.DEAD_LETTER(store, id));
  }
}
