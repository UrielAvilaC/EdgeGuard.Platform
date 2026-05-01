import { BaseFilter } from '../../../shared/models/filter.model';

export interface Patient {
  id: string;
  patientDicomId: string;
  patientName: string;
  birthDate: string | null;
  sex: string | null;
  phoneNumber: string | null;
  email: string | null;
  issuerOfPatientId: string | null;
  facilitySource: string | null;
  createdByNodeId: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface PatientFilter extends BaseFilter {
  createdByNodeId?: string;
  isActive?: boolean;
  hasPhone?: boolean;
  hasEmail?: boolean;
}

export interface UpdatePatientRequest {
  patientName?: string;
  birthDate?: string;
  sex?: string;
  phoneNumber?: string;
  email?: string;
}

export interface ImportResult {
  totalRecords: number;
  successCount: number;
  errorCount: number;
  rows: ImportRowResult[];
}

export interface ImportRowResult {
  rowNumber: number;
  status: string;
  identifier: string | null;
  error: string | null;
}

export const SEX_OPTIONS: { value: string; label: string }[] = [
  { value: 'M', label: 'Masculino' },
  { value: 'F', label: 'Femenino' },
  { value: 'O', label: 'Otro' },
];
