import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ENVIRONMENT } from '../config/environment.config';

type HttpOptions = {
  params?: Record<string, string | number | boolean | undefined | null>;
  headers?: Record<string, string>;
};

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  get<T>(path: string, options?: HttpOptions): Observable<T> {
    return this.http.get<T>(this.url(path), {
      params: this.buildParams(options?.params),
      headers: options?.headers,
    });
  }

  post<T>(path: string, body?: unknown, options?: HttpOptions): Observable<T> {
    return this.http.post<T>(this.url(path), body ?? null, {
      params: this.buildParams(options?.params),
      headers: options?.headers,
    });
  }

  put<T>(path: string, body?: unknown, options?: HttpOptions): Observable<T> {
    return this.http.put<T>(this.url(path), body ?? null, {
      params: this.buildParams(options?.params),
      headers: options?.headers,
    });
  }

  delete<T = void>(path: string, options?: HttpOptions): Observable<T> {
    return this.http.delete<T>(this.url(path), {
      params: this.buildParams(options?.params),
      headers: options?.headers,
    });
  }

  getBlob(path: string, options?: HttpOptions): Observable<HttpResponse<Blob>> {
    return this.http.get(this.url(path), {
      params: this.buildParams(options?.params),
      headers: options?.headers,
      responseType: 'blob',
      observe: 'response',
    });
  }

  postFormData<T>(path: string, formData: FormData): Observable<T> {
    return this.http.post<T>(this.url(path), formData);
  }

  private url(path: string): string {
    return `${this.env.apiBaseUrl}${path}`;
  }

  private buildParams(params?: Record<string, string | number | boolean | undefined | null>): HttpParams {
    let httpParams = new HttpParams();
    if (!params) return httpParams;

    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return httpParams;
  }
}
