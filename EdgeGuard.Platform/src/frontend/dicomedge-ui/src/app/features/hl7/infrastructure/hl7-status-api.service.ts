import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { Hl7ListenerStatus, Hl7MessageSummary, Hl7MessageDetail } from '../models/hl7.models';

@Injectable({ providedIn: 'root' })
export class Hl7StatusApiService {
  private readonly api = inject(ApiClient);

  getStatus(): Observable<Hl7ListenerStatus> {
    return this.api.get<Hl7ListenerStatus>(API_ROUTES.HL7_STATUS.STATUS);
  }

  getRecentMessages(count = 10): Observable<Hl7MessageSummary[]> {
    return this.api.get<Hl7MessageSummary[]>(API_ROUTES.HL7_STATUS.RECENT_MESSAGES, {
      params: { count } as Record<string, string | number | boolean | undefined>,
    });
  }

  getMessageDetail(id: string): Observable<Hl7MessageDetail> {
    return this.api.get<Hl7MessageDetail>(API_ROUTES.HL7_STATUS.MESSAGE_DETAIL(id));
  }

  reprocessMessage(id: string): Observable<void> {
    return this.api.post<void>(API_ROUTES.HL7_STATUS.REPROCESS(id), {});
  }
}
