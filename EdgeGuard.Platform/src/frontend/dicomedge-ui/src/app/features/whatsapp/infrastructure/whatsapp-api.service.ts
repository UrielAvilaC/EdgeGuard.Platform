import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import {
  WhatsAppConfigStatus,
  WhatsAppTemplate,
  WhatsAppTemplateTag,
  WhatsAppAutoSendRule,
  WhatsAppNotification,
  CreateWhatsAppTemplateRequest,
  UpdateWhatsAppTemplateRequest,
  CreateWhatsAppAutoSendRuleRequest,
  UpdateWhatsAppAutoSendRuleRequest,
  SendWhatsAppManualRequest,
  SendWhatsAppManualResponse,
} from '../models/whatsapp.models';

@Injectable({ providedIn: 'root' })
export class WhatsAppApiService {
  private readonly api = inject(ApiClient);

  // ── Config & Tags ──
  getConfigStatus(): Observable<WhatsAppConfigStatus> {
    return this.api.get<WhatsAppConfigStatus>(API_ROUTES.WHATSAPP.CONFIG_STATUS);
  }

  getTags(): Observable<WhatsAppTemplateTag[]> {
    return this.api.get<WhatsAppTemplateTag[]>(API_ROUTES.WHATSAPP.TAGS);
  }

  // ── Templates ──
  getTemplates(): Observable<WhatsAppTemplate[]> {
    return this.api.get<WhatsAppTemplate[]>(API_ROUTES.WHATSAPP.TEMPLATES);
  }

  getTemplate(id: string): Observable<WhatsAppTemplate> {
    return this.api.get<WhatsAppTemplate>(API_ROUTES.WHATSAPP.TEMPLATE_BY_ID(id));
  }

  createTemplate(request: CreateWhatsAppTemplateRequest): Observable<WhatsAppTemplate> {
    return this.api.post<WhatsAppTemplate>(API_ROUTES.WHATSAPP.TEMPLATES, request);
  }

  updateTemplate(id: string, request: UpdateWhatsAppTemplateRequest): Observable<WhatsAppTemplate> {
    return this.api.put<WhatsAppTemplate>(API_ROUTES.WHATSAPP.TEMPLATE_BY_ID(id), request);
  }

  deleteTemplate(id: string): Observable<void> {
    return this.api.delete<void>(API_ROUTES.WHATSAPP.TEMPLATE_BY_ID(id));
  }

  // ── Auto-Send Rules ──
  getAutoSendRules(): Observable<WhatsAppAutoSendRule[]> {
    return this.api.get<WhatsAppAutoSendRule[]>(API_ROUTES.WHATSAPP.AUTO_SEND_RULES);
  }

  createAutoSendRule(request: CreateWhatsAppAutoSendRuleRequest): Observable<WhatsAppAutoSendRule> {
    return this.api.post<WhatsAppAutoSendRule>(API_ROUTES.WHATSAPP.AUTO_SEND_RULES, request);
  }

  updateAutoSendRule(id: string, request: UpdateWhatsAppAutoSendRuleRequest): Observable<WhatsAppAutoSendRule> {
    return this.api.put<WhatsAppAutoSendRule>(API_ROUTES.WHATSAPP.AUTO_SEND_RULE_BY_ID(id), request);
  }

  deleteAutoSendRule(id: string): Observable<void> {
    return this.api.delete<void>(API_ROUTES.WHATSAPP.AUTO_SEND_RULE_BY_ID(id));
  }

  // ── Manual Send ──
  sendManual(request: SendWhatsAppManualRequest): Observable<SendWhatsAppManualResponse> {
    return this.api.post<SendWhatsAppManualResponse>(API_ROUTES.WHATSAPP.SEND, request);
  }

  // ── Notifications ──
  getNotificationsByStudy(studyId: string): Observable<WhatsAppNotification[]> {
    return this.api.get<WhatsAppNotification[]>(API_ROUTES.WHATSAPP.NOTIFICATIONS_BY_STUDY(studyId));
  }
}
