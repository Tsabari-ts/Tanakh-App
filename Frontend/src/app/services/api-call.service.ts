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
    return this.http.post(`${this.baseUrl}/api/v1/subscriptions`, subscriptionRequest, { responseType: 'text' });
  }

  updateReadingProgress(readingProgress: any) {
    return this.http.post(`${this.baseUrl}/api/v1/reading-progress`, readingProgress, { responseType: 'text' });
  }
}
