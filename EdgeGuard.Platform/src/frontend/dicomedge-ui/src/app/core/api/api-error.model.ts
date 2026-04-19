export interface ApiError {
  status: number;
  title: string;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export interface ValidationError {
  field: string;
  messages: string[];
}

export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    'title' in error
  );
}

export function parseValidationErrors(error: ApiError): ValidationError[] {
  if (!error.errors) return [];
  return Object.entries(error.errors).map(([field, messages]) => ({
    field,
    messages,
  }));
}
