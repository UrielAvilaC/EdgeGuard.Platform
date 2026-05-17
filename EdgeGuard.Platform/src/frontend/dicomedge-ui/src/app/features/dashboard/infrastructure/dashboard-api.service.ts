import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { DashboardSummary } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly api = inject(ApiClient);

  getSummary(): Observable<DashboardSummary> {
    return this.api.get<DashboardSummary>(API_ROUTES.DASHBOARD.SUMMARY);
  }
}
