import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DriverDetails, DriverSummary, OnboardingStatus } from '../models/driver.model';
import { PaginatedResponse } from '../models/pagination.model';
import { environment } from '../../../environments/environment';

export interface QueryDriversParams {
  onboardingStatus?: OnboardingStatus;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class DriverService {
  private readonly base = environment.apiUrl + '/admin/drivers';

  constructor(private http: HttpClient) {}

  getAll(request: QueryDriversParams): Observable<PaginatedResponse<DriverSummary>> {
    let params = new HttpParams();

    if (request.onboardingStatus) params = params.set('onboardingStatus', request.onboardingStatus);
    if (request.pageNumber) params = params.set('page', request.pageNumber.toString());
    if (request.pageSize) params = params.set('pageCount', request.pageSize.toString());

    return this.http.get<PaginatedResponse<DriverSummary>>(this.base, { params });
  }

  getById(id: number): Observable<DriverDetails> {
    return this.http.get<DriverDetails>(`${this.base}/${id}`);
  }

  approve(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/approve`, {});
  }

  ban(id: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/ban`, { reason });
  }
}
