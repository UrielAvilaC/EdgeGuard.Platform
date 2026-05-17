import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import {
  RoutingRule,
  RoutingRuleFilter,
  CreateRoutingRuleRequest,
  UpdateRoutingRuleRequest,
  UpdatePriorityRequest,
} from '../models/routing-rule.models';

@Injectable({ providedIn: 'root' })
export class RoutingRulesApiService {
  private readonly api = inject(ApiClient);

  getRules(filter: RoutingRuleFilter): Observable<PagedResult<RoutingRule>> {
    return this.api.get<PagedResult<RoutingRule>>(API_ROUTES.ROUTING_RULES.LIST, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }

  getById(id: string): Observable<RoutingRule> {
    return this.api.get<RoutingRule>(API_ROUTES.ROUTING_RULES.BY_ID(id));
  }

  create(request: CreateRoutingRuleRequest): Observable<RoutingRule> {
    return this.api.post<RoutingRule>(API_ROUTES.ROUTING_RULES.LIST, request);
  }

  update(id: string, request: UpdateRoutingRuleRequest): Observable<void> {
    return this.api.put<void>(API_ROUTES.ROUTING_RULES.BY_ID(id), request);
  }

  enable(id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.ROUTING_RULES.ENABLE(id), {});
  }

  disable(id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.ROUTING_RULES.DISABLE(id), {});
  }

  updatePriority(id: string, request: UpdatePriorityRequest): Observable<void> {
    return this.api.put<void>(API_ROUTES.ROUTING_RULES.PRIORITY(id), request);
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(API_ROUTES.ROUTING_RULES.BY_ID(id));
  }
}
