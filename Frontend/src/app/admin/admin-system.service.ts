import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { BannerStatus, FeatureFlag, MaintenanceStatus, SystemHealth } from './admin.models';

@Injectable({
  providedIn: 'root'
})
export class AdminSystemService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/admin/system`;

  constructor(private http: HttpClient) { }

  getHealth(): Observable<SystemHealth> {
    return this.http.get<SystemHealth>(`${this.baseUrl}/health`, { withCredentials: true });
  }

  getMaintenance(): Observable<MaintenanceStatus> {
    return this.http.get<MaintenanceStatus>(`${this.baseUrl}/maintenance`, { withCredentials: true });
  }

  setMaintenance(enabled: boolean, message: string | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/maintenance`, { enabled, message }, { withCredentials: true });
  }

  getBanner(): Observable<BannerStatus | null> {
    return this.http.get<BannerStatus | null>(`${this.baseUrl}/banner`, { withCredentials: true });
  }

  setBanner(text: string, expiresAt: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/banner`, { text, expiresAt }, { withCredentials: true });
  }

  clearBanner(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/banner`, { withCredentials: true });
  }

  getFlags(): Observable<FeatureFlag[]> {
    return this.http.get<FeatureFlag[]>(`${this.baseUrl}/flags`, { withCredentials: true });
  }

  setFlag(name: string, enabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/flags/${encodeURIComponent(name)}`, { enabled }, { withCredentials: true });
  }

  deleteFlag(name: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/flags/${encodeURIComponent(name)}`, { withCredentials: true });
  }
}
