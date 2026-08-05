import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AdminOverview, DateRange } from './admin.models';

@Injectable({
  providedIn: 'root'
})
export class AdminStatsService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/admin/stats`;

  constructor(private http: HttpClient) { }

  getOverview(range: DateRange): Observable<AdminOverview> {
    return this.http.get<AdminOverview>(`${this.baseUrl}/overview`, {
      params: { from: range.from, to: range.to },
      withCredentials: true
    });
  }
}
