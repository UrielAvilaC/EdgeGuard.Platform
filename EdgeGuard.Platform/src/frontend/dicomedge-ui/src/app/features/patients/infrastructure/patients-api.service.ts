import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { API_ROUTES } from '../../../core/api/api-routes';
import { PagedResult } from '../../../shared/models/pagination.model';
import { Patient, PatientFilter, UpdatePatientRequest, ImportResult } from '../models/patient.models';

@Injectable({ providedIn: 'root' })
export class PatientsApiService {
  private readonly api = inject(ApiClient);

  getPatients(filter: PatientFilter): Observable<PagedResult<Patient>> {
    return this.api.get<PagedResult<Patient>>(API_ROUTES.PATIENTS.LIST, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }

  getById(id: string): Observable<Patient> {
    return this.api.get<Patient>(API_ROUTES.PATIENTS.BY_ID(id));
  }

  getByDicomId(dicomId: string): Observable<Patient> {
    return this.api.get<Patient>(API_ROUTES.PATIENTS.BY_DICOM_ID(dicomId));
  }

  search(name: string): Observable<Patient[]> {
    return this.api.get<Patient[]>(API_ROUTES.PATIENTS.SEARCH, { params: { name } });
  }

  getCount(): Observable<{ count: number }> {
    return this.api.get<{ count: number }>(API_ROUTES.PATIENTS.COUNT);
  }

  update(id: string, request: UpdatePatientRequest): Observable<Patient> {
    return this.api.put<Patient>(API_ROUTES.PATIENTS.BY_ID(id), request);
  }

  export(filter: PatientFilter): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.api.getBlob(API_ROUTES.PATIENTS.EXPORT, {
      params: filter as Record<string, string | number | boolean | undefined>,
    });
  }

  import(file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.api.postFormData<ImportResult>(API_ROUTES.PATIENTS.IMPORT, formData);
  }
}
