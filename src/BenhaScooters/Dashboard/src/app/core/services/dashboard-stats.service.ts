import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminDashboardStatsResponse } from '../models/dashboard-stats.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardStatsService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getDashboardStats(): Observable<AdminDashboardStatsResponse> {
    return this.http.get<AdminDashboardStatsResponse>(`${this.apiUrl}/admin/dashboard-stats`);
  }
}
