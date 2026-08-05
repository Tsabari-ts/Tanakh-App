import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiCallService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http:HttpClient) { }

    getHolidays() {
    return this.http.get<any>(`${this.baseUrl}/JewishCalendar/getJewishCalendar`);
  }

  getVerses(book:string, chapter: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/Tanakh/books/${book}/${chapter}`);
  }

  getBookList(section:string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/Tanakh/books/${section}`);
  }

  getBookByTitle(book:string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/Tanakh/books/main/${book}`);
  }

  subscribe(subscriptionRequest: any) {
    return this.http.post<{ manageToken: string }>(`${this.baseUrl}/api/v1/subscriptions`, subscriptionRequest);
  }

  getReminderPreferences(manageToken: string) {
    return this.http.get<any>(`${this.baseUrl}/api/v1/subscriptions/me`, { params: { token: manageToken } });
  }

  updateReminderPreferences(manageToken: string, preferredTime?: string, action?: 'pause' | 'resume') {
    return this.http.post(`${this.baseUrl}/api/v1/subscriptions/me`, {
      token: manageToken,
      preferredTime: preferredTime ?? null,
      action: action ?? null
    });
  }

  unsubscribeReminder(manageToken: string) {
    return this.http.post(`${this.baseUrl}/api/v1/subscriptions/me/unsubscribe`, { token: manageToken });
  }

  updateReadingProgress(readingProgress: any) {
    return this.http.post(`${this.baseUrl}/api/v1/reading-progress`, readingProgress, { responseType: 'text' });
  }

  getMaintenanceStatus(): Observable<{ enabled: boolean; message: string | null }> {
    return this.http.get<{ enabled: boolean; message: string | null }>(`${this.baseUrl}/api/v1/system/maintenance`);
  }

  getAnnouncementBanner(): Observable<{ text: string; expiresAt: string } | null> {
    return this.http.get<{ text: string; expiresAt: string } | null>(`${this.baseUrl}/api/v1/system/banner`);
  }
}
