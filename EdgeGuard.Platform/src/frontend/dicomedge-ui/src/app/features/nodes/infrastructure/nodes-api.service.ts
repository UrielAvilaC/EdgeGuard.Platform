import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import { Node, NodeFilter, CreateNodeRequest, UpdateNodeRequest } from '../models/node.models';

@Injectable({ providedIn: 'root' })
export class NodesApiService {
  private readonly api = inject(ApiClient);

  getNodes(filter: NodeFilter): Observable<PagedResult<Node>> {
    return this.api.get<PagedResult<Node>>(API_ROUTES.NODES.LIST, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }

  getAll(): Observable<Node[]> {
    return this.api.get<Node[]>(API_ROUTES.NODES.ALL);
  }

  getActive(): Observable<Node[]> {
    return this.api.get<Node[]>(API_ROUTES.NODES.ACTIVE);
  }

  getById(id: string): Observable<Node> {
    return this.api.get<Node>(API_ROUTES.NODES.BY_ID(id));
  }

  create(request: CreateNodeRequest): Observable<Node> {
    return this.api.post<Node>(API_ROUTES.NODES.LIST, request);
  }

  update(id: string, request: UpdateNodeRequest): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODES.BY_ID(id), request);
  }

  enable(id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODES.ENABLE(id), {});
  }

  disable(id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODES.DISABLE(id), {});
  }

  getCount(): Observable<{ count: number }> {
    return this.api.get<{ count: number }>(API_ROUTES.NODES.COUNT);
  }
}
