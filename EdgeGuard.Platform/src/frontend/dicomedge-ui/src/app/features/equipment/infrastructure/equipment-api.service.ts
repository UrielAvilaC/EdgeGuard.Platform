import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import {
  Modality,
  NodeEquipment,
  CreateNodeEquipmentRequest,
  UpdateNodeEquipmentRequest,
} from '../models/equipment.models';

@Injectable({ providedIn: 'root' })
export class EquipmentApiService {
  private readonly api = inject(ApiClient);

  getModalities(supportedOnly = true): Observable<Modality[]> {
    return this.api.get<Modality[]>(API_ROUTES.MODALITIES.LIST(supportedOnly));
  }

  getByNode(nodeId: string): Observable<NodeEquipment[]> {
    return this.api.get<NodeEquipment[]>(API_ROUTES.NODE_EQUIPMENT.BY_NODE(nodeId));
  }

  create(nodeId: string, request: CreateNodeEquipmentRequest): Observable<NodeEquipment> {
    return this.api.post<NodeEquipment>(API_ROUTES.NODE_EQUIPMENT.BY_NODE(nodeId), request);
  }

  update(nodeId: string, id: string, request: UpdateNodeEquipmentRequest): Observable<NodeEquipment> {
    return this.api.put<NodeEquipment>(API_ROUTES.NODE_EQUIPMENT.BY_ID(nodeId, id), request);
  }

  enable(nodeId: string, id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODE_EQUIPMENT.ENABLE(nodeId, id), {});
  }

  disable(nodeId: string, id: string): Observable<void> {
    return this.api.put<void>(API_ROUTES.NODE_EQUIPMENT.DISABLE(nodeId, id), {});
  }

  delete(nodeId: string, id: string): Observable<void> {
    return this.api.delete<void>(API_ROUTES.NODE_EQUIPMENT.BY_ID(nodeId, id));
  }
}
