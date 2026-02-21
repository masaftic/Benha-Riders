import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PolicyResponse, PolicyType } from '../models/policy.model';
import { environment } from '../../../environments/environment';



@Injectable({ providedIn: 'root' })
export class PolicyService {
  private readonly baseUrl = environment.apiUrl + '/policyandterms';

  private readonly http = inject(HttpClient);

  getByType(type: PolicyType): Observable<PolicyResponse> {
    return this.http.get<PolicyResponse>(`${this.baseUrl}/${type}`);
  }

  upsert(type: PolicyType, content: string): Observable<PolicyResponse> {
    return this.http.post<PolicyResponse>(this.baseUrl, {
      type,
      content,
    });
  }
}
