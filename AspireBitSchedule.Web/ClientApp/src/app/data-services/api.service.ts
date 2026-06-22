import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { appRuntimeConfig } from '../core/config/app-runtime-config';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = normalizeApiBaseUrl(appRuntimeConfig.apiBaseUrl);

  public get<T>(path: string, params?: Record<string, string | number | boolean | undefined>): Observable<T> {
    return this.httpClient.get<T>(`${this.apiBaseUrl}${path}`, {
      params: this.toHttpParams(params)
    });
  }

  public post<TResponse, TRequest>(path: string, body: TRequest): Observable<TResponse> {
    return this.httpClient.post<TResponse>(`${this.apiBaseUrl}${path}`, body);
  }

  public put<TResponse, TRequest>(path: string, body: TRequest): Observable<TResponse> {
    return this.httpClient.put<TResponse>(`${this.apiBaseUrl}${path}`, body);
  }

  public delete(path: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.apiBaseUrl}${path}`);
  }

  private toHttpParams(params?: Record<string, string | number | boolean | undefined>): HttpParams | undefined {
    if (!params) {
      return undefined;
    }

    let httpParams = new HttpParams();

    for (const [key, value] of Object.entries(params)) {
      if (value === undefined) {
        continue;
      }

      httpParams = httpParams.set(key, String(value));
    }

    return httpParams;
  }
}

function normalizeApiBaseUrl(apiBaseUrl: string | undefined): string {
  if (!apiBaseUrl) {
    return '/api';
  }

  return apiBaseUrl.endsWith('/')
    ? apiBaseUrl.slice(0, -1)
    : apiBaseUrl;
}
