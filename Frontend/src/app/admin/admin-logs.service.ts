import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ErrorLogItem, PagedResult, TopError } from './admin.models';
import { toQueryParams } from './query-params.util';

export interface ErrorLogQuery {
  level?: string;
  from?: string;
  to?: string;
  search?: string;
  page: number;
  limit: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminLogsService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/admin/logs`;

  constructor(private http: HttpClient) { }

  getLogs(query: ErrorLogQuery): Observable<PagedResult<ErrorLogItem>> {
    return this.http.get<PagedResult<ErrorLogItem>>(this.baseUrl, {
      params: toQueryParams(query),
      withCredentials: true
    });
  }

  getTopErrors(): Observable<TopError[]> {
    return this.http.get<TopError[]>(`${this.baseUrl}/top`, { withCredentials: true });
  }

  resolve(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/resolve`, {}, { withCredentials: true });
  }

  cleanup(olderThanDays: number): Observable<{ deleted: number }> {
    return this.http.post<{ deleted: number }>(`${this.baseUrl}/cleanup`, { olderThanDays }, { withCredentials: true });
  }

  exportParams(query: Omit<ErrorLogQuery, 'page' | 'limit'>): Record<string, string> {
    return toQueryParams(query);
  }
}
