import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import { AssignPacsRequest, Node, NodeFilter, CreateNodeRequest, UpdateNodeRequest, NodeTelemetry, NodePacsCEchoStatus } from '../models/node.models';

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

  assignPacs(nodeId: string, pacsId: string, request: AssignPacsRequest): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODES.ASSIGN_PACS(nodeId, pacsId), request);
  }

  unassignPacs(nodeId: string, pacsId: string): Observable<void> {
    return this.api.delete<void>(API_ROUTES.NODES.UNASSIGN_PACS(nodeId, pacsId));
  }

  getTelemetry(nodeId: string, limit = 50): Observable<NodeTelemetry[]> {
    return this.api.get<NodeTelemetry[]>(API_ROUTES.NODES.TELEMETRY(nodeId), {
      params: { limit } as Record<string, string | number | boolean | undefined>,
    });
  }

  /**
   * Returns the latest PACS C-ECHO connectivity status for a node as reported
   * to the Hub by the node's periodic C-ECHO cycle.
   * Returns 404 if the node has not yet reported any results.
   */
  getPacsEchoStatus(nodeId: string): Observable<NodePacsCEchoStatus> {
    return this.api.get<NodePacsCEchoStatus>(API_ROUTES.NODES.PACS_ECHO(nodeId));
  }
}
