import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiCallService {
  constructor(private http:HttpClient) { }

    getHolidays() {
    return this.http.get<any>(`https://localhost:44308/JewishCalendar/getJewishCalendar`);
  }

  getVerses(book:string, chapter: string): Observable<any> {
    return this.http.get<any>(`https://localhost:44308/Tanakh/books/${book}/${chapter}`);
  }

  getBookList(section:string): Observable<any> {
    return this.http.get<any>(`https://localhost:44308/Tanakh/books/${section}`);
  }  

  RegisterNewUser(userData: any) {
    return this.http.post<any>(`https://localhost:44308/Subscribe/RegisterUser`, userData);
  }

  DeleteSubscribedUser(unSubscribe: any) {
    return this.http.post<any>(`https://localhost:44308/Subscribe/DeleteUser`, unSubscribe);
  }
}
