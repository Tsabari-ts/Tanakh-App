import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminAuthService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/admin/auth`;

  constructor(private http: HttpClient) { }

  login(username: string, password: string): Observable<{ otpRequired: boolean }> {
    return this.http.post<{ otpRequired: boolean }>(
      `${this.baseUrl}/login`, { username, password }, { withCredentials: true });
  }

  verifyOtp(code: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/verify-otp`, { code }, { withCredentials: true });
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {}, { withCredentials: true });
  }

  // No side effect - just probes whether the session cookie is still valid.
  // Used by the route guard, not by the login/logout flow itself.
  checkSession(): Observable<void> {
    return this.http.get<void>(`${this.baseUrl}/session`, { withCredentials: true });
  }
}
