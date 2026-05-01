import { BaseFilter } from '../../../shared/models/filter.model';

export interface RoutingRule {
  id: string;
  name: string;
  priority: number;
  isEnabled: boolean;
  matchMessageType: string | null;
  matchTriggerEvent: string | null;
  matchSendingFacility: string | null;
  matchSendingApplication: string | null;
  targetNodeId: string;
  matchCount: number;
  lastMatchedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface RoutingRuleFilter extends BaseFilter {
  isEnabled?: boolean;
  targetNodeId?: string;
}

export interface CreateRoutingRuleRequest {
  name: string;
  targetNodeId: string;
  priority?: number;
  matchMessageType?: string;
  matchTriggerEvent?: string;
  matchSendingFacility?: string;
  matchSendingApplication?: string;
}

export interface UpdateRoutingRuleRequest {
  name: string;
  targetNodeId: string;
  priority?: number;
  matchMessageType?: string;
  matchTriggerEvent?: string;
  matchSendingFacility?: string;
  matchSendingApplication?: string;
}

export interface UpdatePriorityRequest {
  priority: number;
}
