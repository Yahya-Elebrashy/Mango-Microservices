import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { ResponseDto } from '../../models';

export type ApiContentType = 'json' | 'multipart';

export interface RequestOptions {
  url: string;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  data?: unknown;
  contentType?: ApiContentType;
}

@Injectable({ providedIn: 'root' })
export class ApiService {

  constructor(private http: HttpClient) {}

  send<T = unknown>(options: RequestOptions): Observable<ResponseDto<T>> {
    const { url, method = 'GET', data, contentType = 'json' } = options;

    let body: FormData | string | null = null;

    if (data) {
      if (contentType === 'multipart') {
        body = this.toFormData(data as Record<string, unknown>);
      } else {
        body = JSON.stringify(data);
      }
    }

    const headers = contentType === 'json'
      ? new HttpHeaders({ 'Content-Type': 'application/json' })
      : new HttpHeaders(); 

    const request$ = this.http.request<ResponseDto<T>>(method, url, {
      body,
      headers,
    });

    return request$.pipe(
      catchError(err => {
        const fallback: ResponseDto<T> = {
          result: null,
          isSuccess: false,
          message: err?.error?.message ?? err?.message ?? 'An error occurred',
        };
        return of(fallback);
      })
    );
  }

  // Convenience methods
  get<T>(url: string): Observable<ResponseDto<T>> {
    return this.send<T>({ url, method: 'GET' });
  }

  post<T>(url: string, data: unknown, contentType: ApiContentType = 'json'): Observable<ResponseDto<T>> {
    return this.send<T>({ url, method: 'POST', data, contentType });
  }

  put<T>(url: string, data: unknown, contentType: ApiContentType = 'json'): Observable<ResponseDto<T>> {
    return this.send<T>({ url, method: 'PUT', data, contentType });
  }

  delete<T>(url: string): Observable<ResponseDto<T>> {
    return this.send<T>({ url, method: 'DELETE' });
  }

  private toFormData(data: Record<string, unknown>): FormData {
    const formData = new FormData();
    for (const key of Object.keys(data)) {
      const value = data[key];
      if (value instanceof File) {
        formData.append(key, value, value.name);
      } else if (value !== null && value !== undefined) {
        formData.append(key, String(value));
      }
    }
    return formData;
  }
}