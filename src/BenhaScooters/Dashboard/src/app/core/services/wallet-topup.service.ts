import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  GetTopUpRequestsParams,
  ReviewTopUpRequestPayload,
  TopUpRequest
} from '../models/wallet-topup.model';
import { PaginatedResponse } from '../models/pagination.model';

@Injectable({ providedIn: 'root' })
export class WalletTopUpService {
  private readonly base = `${environment.apiUrl}/admin/drivers/wallet/topup`;

  constructor(private readonly http: HttpClient) {}

  getRequests(request: GetTopUpRequestsParams): Observable<PaginatedResponse<TopUpRequest>> {
    let params = new HttpParams();

    if (request.page) params = params.set('page', request.page.toString());
    if (request.pageSize) params = params.set('pageSize', request.pageSize.toString());
    if (request.status) params = params.set('status', request.status);
    if (request.driverId) params = params.set('driverId', request.driverId.toString());

    return this.http.get<PaginatedResponse<TopUpRequest>>(this.base, { params });
  }

  review(requestId: number, payload: ReviewTopUpRequestPayload): Observable<void> {
    return this.http.post<void>(`${this.base}/${requestId}/review`, payload);
  }
}
