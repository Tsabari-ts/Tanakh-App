import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { downloadBlob } from './shared/download-blob.util';

@Injectable({
  providedIn: 'root'
})
export class AdminExportService {
  constructor(private http: HttpClient) { }

  download(resource: 'users' | 'sms-log' | 'error-log', params: Record<string, string>) {
    return this.http.get(`${environment.apiUrl}/api/v1/admin/export/${resource}`, {
      params,
      withCredentials: true,
      responseType: 'blob'
    }).pipe(
      tap(blob => downloadBlob(blob, `${resource}.csv`)));
  }
}
