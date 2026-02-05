import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { AuthService } from './auth.service';
import {
  PagedResult,
  DriverListItem,
  DriverDetails,
  LoginRequest,
  LoginResponse
} from '../models/driver.models';


@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly baseUrl = '/api';

  login(phoneNumber: string, password: string): Observable<LoginResponse> {
    return this.http.post<{ type: string, result: LoginResponse }>(`${this.baseUrl}/auth/login`, {
      phoneNumber,
      password
    }).pipe(map((response: { type: string, result: LoginResponse}) => response.result));
  }

  getDrivers(
    pageNumber: number = 1,
    pageSize: number = 20,
    status?: string,
    searchTerm?: string
  ): Observable<PagedResult<DriverListItem>> {
    let url = `${this.baseUrl}/admin/drivers?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (status) url += `&onboardingStatus=${status}`;
    if (searchTerm) url += `&searchTerm=${searchTerm}`;

    return this.http.get<PagedResult<DriverListItem>>(url, {
      headers: this.getHeaders()
    });
  }

  getDriverDetails(userId: number): Observable<DriverDetails> {
    return this.http.get<DriverDetails>(`${this.baseUrl}/admin/drivers/${userId}`, {
      headers: this.getHeaders()
    });
  }

  approveDriver(userId: number): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/admin/drivers/${userId}/approve`,
      {},
      { headers: this.getHeaders() }
    );
  }

  rejectDriver(userId: number, reason: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/admin/drivers/${userId}/reject`,
      { rejectionReason: reason },
      { headers: this.getHeaders() }
    );
  }

  banDriver(userId: number, reason: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/admin/drivers/${userId}/ban`,
      { reason },
      { headers: this.getHeaders() }
    );
  }

  unbanDriver(userId: number): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/admin/drivers/${userId}/unban`,
      {},
      { headers: this.getHeaders() }
    );
  }

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json'
    });
  }
}
