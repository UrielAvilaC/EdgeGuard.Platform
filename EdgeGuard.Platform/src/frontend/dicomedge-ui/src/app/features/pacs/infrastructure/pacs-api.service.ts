import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import {
  PacsServer,
  PacsServerFilter,
  CreatePacsServerRequest,
  UpdatePacsServerRequest,
} from '../models/pacs.models';

@Injectable({ providedIn: 'root' })
export class PacsApiService {
  private readonly api = inject(ApiClient);

  getPacsServers(filter: PacsServerFilter): Observable<PagedResult<PacsServer>> {
    return this.api.get<PagedResult<PacsServer>>(API_ROUTES.PACS_SERVERS.LIST, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }

  getById(id: string): Observable<PacsServer> {
    return this.api.get<PacsServer>(API_ROUTES.PACS_SERVERS.BY_ID(id));
  }

  create(request: CreatePacsServerRequest): Observable<PacsServer> {
    return this.api.post<PacsServer>(API_ROUTES.PACS_SERVERS.LIST, request);
  }

  update(id: string, request: UpdatePacsServerRequest): Observable<void> {
    return this.api.put<void>(API_ROUTES.PACS_SERVERS.BY_ID(id), request);
  }

  enable(id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.PACS_SERVERS.ENABLE(id), {});
  }

  disable(id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.PACS_SERVERS.DISABLE(id), {});
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(API_ROUTES.PACS_SERVERS.BY_ID(id));
  }
}
