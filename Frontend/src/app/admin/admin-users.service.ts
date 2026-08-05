import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult, UserListItem } from './admin.models';
import { toQueryParams } from './query-params.util';

export interface UsersQuery {
  search?: string;
  status?: string;
  from?: string;
  to?: string;
  page: number;
  limit: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminUsersService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/admin/users`;

  constructor(private http: HttpClient) { }

  getUsers(query: UsersQuery): Observable<PagedResult<UserListItem>> {
    return this.http.get<PagedResult<UserListItem>>(this.baseUrl, {
      params: toQueryParams(query),
      withCredentials: true
    });
  }

  block(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}`, { action: 'block' }, { withCredentials: true });
  }

  unblock(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}`, { action: 'unblock' }, { withCredentials: true });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`, { withCredentials: true });
  }

  exportParams(query: Omit<UsersQuery, 'page' | 'limit'>): Record<string, string> {
    return toQueryParams(query);
  }
}
