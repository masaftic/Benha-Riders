import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ParentDetails, ParentSummary } from '../models/parent.model';
import { PaginationRequest, PaginatedResponse } from '../models/pagination.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ParentService {
  private readonly base = environment.apiUrl + '/parents';

  constructor(private http: HttpClient) {}

  getAll(request: PaginationRequest): Observable<PaginatedResponse<ParentSummary>> {
    let params = new HttpParams();

    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber.toString());
    if (request.pageSize) params = params.set('pageSize', request.pageSize.toString());
    if (request.search) params = params.set('search', request.search);
    if (request.orderBy) params = params.set('orderBy', request.orderBy);
    if (request.isDescending !== undefined) params = params.set('isDescending', request.isDescending.toString());

    return this.http.get<PaginatedResponse<ParentSummary>>(this.base, { params });
  }

  getById(id: string): Observable<ParentDetails> {
    return this.http.get<ParentDetails>(`${this.base}/${id}`);
  }
}
