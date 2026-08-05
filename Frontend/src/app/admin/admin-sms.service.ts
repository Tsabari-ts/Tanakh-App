import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult, SmsBalanceInfo, SmsLogItem, SmsStats } from './admin.models';
import { toQueryParams } from './query-params.util';

export interface SmsLogQuery {
  status?: string;
  from?: string;
  to?: string;
  page: number;
  limit: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminSmsService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/admin/sms`;

  constructor(private http: HttpClient) { }

  getBalance(): Observable<SmsBalanceInfo> {
    return this.http.get<SmsBalanceInfo>(`${this.baseUrl}/balance`, { withCredentials: true });
  }

  getStats(): Observable<SmsStats> {
    return this.http.get<SmsStats>(`${this.baseUrl}/stats`, { withCredentials: true });
  }

  getLog(query: SmsLogQuery): Observable<PagedResult<SmsLogItem>> {
    return this.http.get<PagedResult<SmsLogItem>>(`${this.baseUrl}/log`, {
      params: toQueryParams(query),
      withCredentials: true
    });
  }

  sendTest(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/test`, {}, { withCredentials: true });
  }

  exportParams(query: Omit<SmsLogQuery, 'page' | 'limit'>): Record<string, string> {
    return toQueryParams(query);
  }
}
