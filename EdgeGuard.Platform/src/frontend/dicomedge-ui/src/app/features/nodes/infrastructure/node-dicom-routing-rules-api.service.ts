import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import {
  NodeDicomRoutingRule,
  CreateNodeDicomRoutingRuleRequest,
  UpdateNodeDicomRoutingRuleRequest,
} from '../models/node-dicom-routing-rule.models';

@Injectable({ providedIn: 'root' })
export class NodeDicomRoutingRulesApiService {
  private readonly api = inject(ApiClient);

  getByNode(nodeId: string): Observable<NodeDicomRoutingRule[]> {
    return this.api.get<NodeDicomRoutingRule[]>(
      API_ROUTES.NODE_DICOM_ROUTING_RULES.BY_NODE(nodeId),
    );
  }

  create(nodeId: string, request: CreateNodeDicomRoutingRuleRequest): Observable<NodeDicomRoutingRule> {
    return this.api.post<NodeDicomRoutingRule>(
      API_ROUTES.NODE_DICOM_ROUTING_RULES.BY_NODE(nodeId),
      request,
    );
  }

  update(nodeId: string, id: string, request: UpdateNodeDicomRoutingRuleRequest): Observable<NodeDicomRoutingRule> {
    return this.api.put<NodeDicomRoutingRule>(
      API_ROUTES.NODE_DICOM_ROUTING_RULES.BY_ID(nodeId, id),
      request,
    );
  }

  enable(nodeId: string, id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODE_DICOM_ROUTING_RULES.ENABLE(nodeId, id), {});
  }

  disable(nodeId: string, id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODE_DICOM_ROUTING_RULES.DISABLE(nodeId, id), {});
  }

  updatePriority(nodeId: string, id: string, priority: number): Observable<void> {
    return this.api.put<void>(
      API_ROUTES.NODE_DICOM_ROUTING_RULES.PRIORITY(nodeId, id),
      { priority },
    );
  }

  delete(nodeId: string, id: string): Observable<void> {
    return this.api.delete<void>(API_ROUTES.NODE_DICOM_ROUTING_RULES.BY_ID(nodeId, id));
  }
}
